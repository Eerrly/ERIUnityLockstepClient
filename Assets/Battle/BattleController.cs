using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 战斗控制器
/// </summary>
public class BattleController
{
    /// <summary>
    /// 客户端时间与服务器预估时间的最小时间差
    /// </summary>
    private long _timeOffset;
    /// <summary>
    /// 客户端计时器
    /// </summary>
    private readonly Stopwatch _stopwatch;
    /// <summary>
    /// 已预测战斗实体队列
    /// </summary>
    private readonly Queue<BattleEntity> _predictBattleEntityQueue;
    /// <summary>
    /// 已预测帧数据队列 （由最近一次发给服务器的操作数据组成的一个帧数据队列）
    /// </summary>
    private readonly Queue<FrameBuffer.Frame> _predictFrameQueue;
    /// <summary>
    /// 服务器返回的帧数据队列
    /// </summary>
    private readonly Queue<FrameBuffer.Frame> _serverFrameQueue;
    /// <summary>
    /// 战斗实体缓存池子
    /// </summary>
    private readonly BattleEntityPool _battleEntityPool;
    /// <summary>
    /// 将要发送给服务器的帧号
    /// </summary>
    private uint _willSentFrame;
    /// <summary>
    /// 最近一次发送给服务器的帧号
    /// </summary>
    private uint _lastSentFrame;
    /// <summary>
    /// 最近一次发送给服务器的操作数据
    /// </summary>
    private FrameBuffer.Input _lastSentInput;
    /// <summary>
    /// 最近一次使用的服务器返回的帧数据
    /// </summary>
    private FrameBuffer.Frame _lastNetworkFrame;
    /// <summary>
    /// 最近一次执行逻辑轮询的时间戳
    /// </summary>
    private long _lastProcessFrameTime;
    /// <summary>
    /// 最近一次执行心跳的时间戳
    /// </summary>
    private long _lastHeartbeatTime;

    /// <summary>
    /// 战斗渲染实体
    /// </summary>
    private BattleEntity _displayBattleEntity;
    /// <summary>
    /// 战斗预测实体
    /// </summary>
    private BattleEntity _predictBattleEntity;
    /// <summary>
    /// 战斗确认实体
    /// </summary>
    private BattleEntity _confirmBattleEntity;

    /// <summary>
    /// 战斗渲染实体
    /// </summary>
    public BattleEntity DisplayBattleEntity => _displayBattleEntity;
    /// <summary>
    /// 战斗预测实体
    /// </summary>
    public BattleEntity PredictBattleEntity => _predictBattleEntity;
    /// <summary>
    /// 战斗确认实体
    /// </summary>
    public BattleEntity ConfirmBattleEntity => _confirmBattleEntity;

    /// <summary>
    /// 丢帧次数
    /// </summary>
    private int _lostFrameCount;
    /// <summary>
    /// 最近一次丢帧的帧号
    /// </summary>
    private int _lastLostFrame;

    public BattleController()
    {
        _stopwatch = new Stopwatch();
        _battleEntityPool = new BattleEntityPool();
        _predictBattleEntityQueue = new Queue<BattleEntity>();
        _predictFrameQueue = new Queue<FrameBuffer.Frame>();
        _serverFrameQueue = new Queue<FrameBuffer.Frame>();
    }

    /// <summary>
    /// 初始化所有实体对象
    /// </summary>
    public void InitEntities()
    {
        _lastSentInput = new FrameBuffer.Input(byte.MaxValue);
        _confirmBattleEntity = new BattleEntity(); _confirmBattleEntity.Init();
        _confirmBattleEntity.Name = "Confirm";
        _predictBattleEntity = new BattleEntity(); _predictBattleEntity.Init();
        _predictBattleEntity.Name = "Predict";
        _displayBattleEntity = new BattleEntity(); _displayBattleEntity.Init();
        _displayBattleEntity.Name = "Display";
        for (var i = 0; i < GameManager.Instance.RoomInfo.Gamers.Count; i++)
        {
            var playerEntity = new PlayerEntity(); 
            playerEntity.Init();
            playerEntity.ID = (int)(GameManager.Instance.RoomInfo.Gamers[i] - GameSetting.DefaultPlayerIdBase - 1);
            _confirmBattleEntity.PlayerEntities.Add(playerEntity);
        }
        _confirmBattleEntity.CopyTo(_predictBattleEntity);
        _confirmBattleEntity.CopyTo(_displayBattleEntity);
    }

    /// <summary>
    /// 战斗网络轮询
    /// </summary>
    /// <param name="cancellationToken">任务取消句柄</param>
    /// <returns>任务</returns>
    public Task NetUpdate(CancellationToken cancellationToken)
    {
        var input = InputManager.Instance.GetInput(GameManager.Instance.GetBattlePos());
        if (_willSentFrame != default && !input.Compare(_lastSentInput) && _lastSentFrame != _willSentFrame)
        {
            Logger.Log(LogLevel.Info, $"NetUpdate SendFrame Frame:{_willSentFrame} Input:{input}");
            NetworkManager.Instance.SendBattleFrameMessage(_willSentFrame, input.ToByte());
            _lastSentFrame = _willSentFrame;
            _lastSentInput = input;
        }
        if (_stopwatch.ElapsedMilliseconds - _lastHeartbeatTime >= BattleSetting.HeartbeatTime)
        {
            _lastHeartbeatTime = _stopwatch.ElapsedMilliseconds;
            NetworkManager.Instance.SendBattleHeartBeatMessage(GameManager.Instance.PlayerId, (ulong)_stopwatch.ElapsedMilliseconds);
        }
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    /// <summary>
    /// 战斗逻辑轮询
    /// </summary>
    /// <param name="cancellationToken">任务取消句柄</param>
    /// <returns>任务</returns>
    public Task LogicUpdate(CancellationToken cancellationToken)
    {
        RollbackConfirmAndSyncEntities(ref _lastNetworkFrame);
        
        // 已经执行过逻辑轮询的预测实体的下一帧
        var predictBattleClientFrame = _predictBattleEntity.Frame + 1;
        if (predictBattleClientFrame - GameManager.Instance.ServerAuthorityFrame < BattleSetting.MaxPredictFrameCount)
        {
            // 魔法数字 用来控制逻辑轮询的间隔，控制预测的频率
            var magic = Math.Min(BattleSetting.BattleInterval, _lostFrameCount * 2 + 4);
            // 预估的一个服务器时间
            var estimateServerTime = _stopwatch.ElapsedMilliseconds - _timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            // 用预估的一个服务器时间加上Ping值的一半在加一个魔法数字来判断此时是否服务器已经下发了新的帧数据
            var hasNewFrame = estimateServerTime + NetworkManager.Instance.RealPing * 0.5f + magic >= predictBattleClientFrame * BattleSetting.BattleInterval;
            // 如果2帧内还没有进行逻辑轮询，则也进行逻辑轮询
            var slowdown = _stopwatch.ElapsedMilliseconds - _lastProcessFrameTime >= BattleSetting.BattleInterval * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! predictBattleClientFrame:{predictBattleClientFrame} CurServerFrame:{GameManager.Instance.ServerAuthorityFrame}");
            if (hasNewFrame || slowdown){
                // 将预测帧的下一帧作为发给服务器的操作帧号
                _willSentFrame = (uint)predictBattleClientFrame + 1;
                _lastProcessFrameTime = _stopwatch.ElapsedMilliseconds;
                EnqueueEntityToPredictQueueAndDisplay();
            }
        }

        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    /// <summary>
    /// 回滚、确认并且同步战斗实体
    /// </summary>
    /// <param name="lastServerFrame">最近一次使用的服务器返回的帧数据</param>
    private void RollbackConfirmAndSyncEntities(ref FrameBuffer.Frame lastServerFrame)
    {
        UpdateServerFrameQueue();

        var confirmPredictEntity = _confirmBattleEntity;
        var flag = UpdateConfirmPredictEntity(ref confirmPredictEntity, ref lastServerFrame);

        SyncConfirmPredictEntity(flag, ref confirmPredictEntity, ref lastServerFrame);
    }

    /// <summary>
    /// 更新并且确认战斗实体
    /// 如果之前预测与实际服务器返回的不同，则使用服务器返回的数据组组装成战斗实体从预测不一样的帧号开始重新轮询，并且将其确认
    /// </summary>
    /// <param name="confirmPredictEntity">最近一次被确认的战斗实体</param>
    /// <param name="lastServerFrame">最近一次使用的服务器返回的帧数据</param>
    /// <returns>是否成功更新了之前预测的战斗实体</returns>
    private bool UpdateConfirmPredictEntity(ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        // 服务器返回的帧数据是否和自己预测的帧数据是否一致，如果队列中某一个不一致，后续的实体都按不一致处理
        var flag = false;
        while (_serverFrameQueue.Count > 0)
        {
            lastServerFrame = _serverFrameQueue.Dequeue();
            CalculateDiffBetweenServerAndClientTime(lastServerFrame.frame);

            var tmpInput = new FrameBuffer.Input();
            var tmpSelfInput = lastServerFrame.GetInputByPos(GameManager.Instance.GetBattlePos(), ref tmpInput);
            // 服务器返回的帧号与最近一次发送的帧数据帧号相同，但是操作不同说明该帧数据已被服务器丢弃
            if (lastServerFrame.frame == _lastSentFrame && tmpSelfInput && !tmpInput.Compare(_lastSentInput))
            {
                _lostFrameCount ++;
                _lastLostFrame = lastServerFrame.frame;
            }
            
            if (_predictBattleEntityQueue.Count == 0) flag = true;
            if (_predictBattleEntityQueue.Count > 0)
            {
                var predictFrame = _predictFrameQueue.Dequeue();
                // 比较服务器返回的帧数据是否和自己预测的帧数据一致
                if (CompareFrame(ref lastServerFrame, ref predictFrame) && !flag)
                {
                    confirmPredictEntity = _predictBattleEntityQueue.Dequeue();
                }
                else
                {
                    // 预测实体队列回收到池子里，既然不一致已无用
                    _battleEntityPool.Enqueue(_predictBattleEntityQueue.Dequeue());
                    flag = true;
                }
            }
            // 不一致，则使用服务器返回的帧数据更新并回滚最近一次确认实体重新执行逻辑轮询
            if (flag)
            {
                UpdateEntityInput(confirmPredictEntity, ref lastServerFrame);
                confirmPredictEntity.Frame++;
                UpdateEntityState(confirmPredictEntity);
            }
            
            // 更新最近一次已确认的战斗实体
            confirmPredictEntity.CopyTo(_confirmBattleEntity);
            // 战斗同步校验
            if (confirmPredictEntity.Frame != 0 && confirmPredictEntity.Frame % BattleSetting.Md5CheckFrame == 0)
            {
                var md5 = 0L;
                foreach (var entity in confirmPredictEntity.PlayerEntities)
                    md5 ^= (entity.Transform.pos.x._raw ^ entity.Transform.pos.z._raw);
                NetworkManager.Instance.SendBattleCheckMessage(confirmPredictEntity.Frame, GameManager.Instance.GetBattlePos(), (int)md5);
            }
            // 战斗记录，用于回放
            BattleRecordManager.Instance.RecordBattleEntity(confirmPredictEntity);
        }
        return flag;
    }

    /// <summary>
    /// 同步并且确认预测战斗实体
    /// </summary>
    /// <param name="flag">服务器返回的帧数据是否和自己预测的帧数据是否一致</param>
    /// <param name="confirmPredictEntity">最近一次被确认的战斗实体</param>
    /// <param name="lastServerFrame">最近一次使用的服务器返回的帧数据</param>
    private void SyncConfirmPredictEntity(bool flag, ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        if (confirmPredictEntity != _confirmBattleEntity)
        {
            confirmPredictEntity.CopyTo(_confirmBattleEntity);
            confirmPredictEntity = _confirmBattleEntity;
        }
        if (flag)
        {
            // 使用最近一次被确认的战斗实体作为预测战斗实体
            confirmPredictEntity.CopyTo(_predictBattleEntity);

            var count = _predictFrameQueue.Count;
            for (int i = 0; i < count; i++)
            {
                // 将预测的帧数据操作都修改为服务器返回的帧数据操作
                var predictInputFrame = _predictFrameQueue.Dequeue();
                for (int j = 0; j < predictInputFrame.Length; j++)
                {
                    var input = new FrameBuffer.Input();
                    if (lastServerFrame.GetInputByPos(predictInputFrame[j].pos, ref input))
                        predictInputFrame.SetInputByPos(input.pos, input);
                }
                _predictFrameQueue.Enqueue(predictInputFrame);

                // 以更新后的帧数据操作来再次执行战斗逻辑轮询
                var predictEntity = _predictBattleEntityQueue.Dequeue();
                _predictBattleEntity.Frame++;
                UpdateEntityInput(_predictBattleEntity, ref predictInputFrame);
                UpdateEntityState(_predictBattleEntity);
                _predictBattleEntity.CopyTo(predictEntity);
                _predictBattleEntityQueue.Enqueue(predictEntity);

                // 并将最后一个预测实体作为渲染实体渲染
                if(i == count -1) predictEntity.CopyTo(_displayBattleEntity);
            }
        }
    }

    /// <summary>
    /// 更新服务器返回的帧数据队列
    /// </summary>
    private void UpdateServerFrameQueue()
    {
        _serverFrameQueue.Clear();
        var inputFrame = FrameBuffer.Frame.defFrame;
        var frame = _confirmBattleEntity.Frame;
        // 从服务器返回的帧缓存中取出渲染实体预测所花费数量的帧数据
        for (int i = 0; i < BattleSetting.MaxPredictFrameCount; i++)
        {
            if(frame++ > GameManager.Instance.ServerAuthorityFrame)
                break;
            if(!GameManager.Instance.FrameBuffer.TryGetFrame(frame, ref inputFrame))
                break;
            _serverFrameQueue.Enqueue(inputFrame);
        }

        if (GameManager.Instance.FrameBuffer.LastSetFrameIndex - frame > BattleSetting.MaxPredictFrameCount)
            _lostFrameCount = 0;
        if (_lostFrameCount > 0 && frame - _lastLostFrame >= BattleSetting.BattleInterval * 2)
            _lostFrameCount --;
    }

    /// <summary>
    /// 计算客户端时间与服务器预估时间的一个最小时间差
    /// </summary>
    /// <param name="frame">帧号（用于初始化计时器）</param>
    private void CalculateDiffBetweenServerAndClientTime(int frame)
    {
        if (frame == 0) 
        {
            _stopwatch.Start();
            _timeOffset = 0;
        }
        var timeOffsetShouldBe = _stopwatch.ElapsedMilliseconds - GameManager.Instance.ServerAuthorityFrame * BattleSetting.BattleInterval;
        if (timeOffsetShouldBe < _timeOffset) 
            _timeOffset = timeOffsetShouldBe;
    }

    /// <summary>
    /// 比较帧数据
    /// </summary>
    /// <param name="networkFrame">服务器返回的帧数据</param>
    /// <param name="predictFrame">预测的帧数据</param>
    /// <returns>帧数据是否一致</returns>
    private bool CompareFrame(ref FrameBuffer.Frame networkFrame, ref FrameBuffer.Frame predictFrame)
    {
        if (networkFrame.frame != predictFrame.frame)
            return false;
        for (int i = 0; i < networkFrame.Length; i++)
        {
            var otherInput = new FrameBuffer.Input();
            var netInput = networkFrame[i];
            if (predictFrame.GetInputByPos(netInput.pos, ref otherInput) && !otherInput.Compare(netInput))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 使用预测的帧号和最后一次发给服务器的操作数据组成帧数据，再以此来合成预测实体来进行逻辑轮询以及渲染
    /// </summary>
    private void EnqueueEntityToPredictQueueAndDisplay()
    {
        var newEntity = _battleEntityPool.Dequeue();

        _predictBattleEntity.Frame++;
        _lastNetworkFrame.frame = _predictBattleEntity.Frame;
        // 如果为byte.MaxValue的话说明玩家并没有操作，所以这里不需要再更新操作数据，它本身就是上一次的操作
        if (_lastSentInput.ToByte() != byte.MaxValue)
            _lastNetworkFrame.SetInputByPos(_lastSentInput.pos, _lastSentInput);
        UpdateEntityInput(_predictBattleEntity, ref _lastNetworkFrame);
        UpdateEntityState(_predictBattleEntity);

        newEntity.Name = "Predict";
        _predictBattleEntity.CopyTo(newEntity);
        _predictBattleEntityQueue.Enqueue(newEntity);
        _predictFrameQueue.Enqueue(_lastNetworkFrame);

        // 用预测实体来渲染
        newEntity.CopyTo(_displayBattleEntity);
    }

    /// <summary>
    /// 通过帧数据来给战斗实体内的玩家实体操作数据更新赋值
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
    /// <param name="inputFrame">帧数据</param>
    private void UpdateEntityInput(BattleEntity battleEntity, ref FrameBuffer.Frame inputFrame)
    {
        var playerEntities = battleEntity.PlayerEntities;
        for (var i = 0; i < playerEntities.Count; i++)
        {
            var inputComponent = playerEntities[i].Input;
            var input = new FrameBuffer.Input();
            if (inputFrame.GetInputByPos(playerEntities[i].ID, ref input))
            {
                inputComponent.yaw = input.yaw - FixedMath.YawOffset;
                inputComponent.key = input.key;
            }
        }
    }

    /// <summary>
    /// 战斗实体的逻辑轮询
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
    private void UpdateEntityState(BattleEntity battleEntity)
    {
        battleEntity.Time += FrameEngine.FrameInterval;
        var playerEntities = battleEntity.PlayerEntities;
        AnimationSystem.UpdateEvent(battleEntity);
        foreach (var entity in playerEntities) PlayerStateMachine.Instance.Update(entity, battleEntity);
        foreach (var entity in playerEntities) PlayerStateMachine.Instance.LateUpdate(entity, battleEntity);
        foreach (var entity in playerEntities) PlayerStateMachine.Instance.DoChangeState(entity, battleEntity);
        PhysicsSystem.PreUpdate(battleEntity);
        PhysicsSystem.Update(battleEntity);
        foreach (var entity in playerEntities) MoveSystem.TransformLogicUpdate(entity, FixedNumber.MakeFixNum(BattleSetting.BattleInterval, 1000));
        BattleStateMachine.Instance.Update(battleEntity, null);
    }
    

}