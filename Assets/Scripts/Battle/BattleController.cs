using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 战斗管理器
/// </summary>
public class BattleController
{
    /// <summary>
    /// 战斗线程任务
    /// </summary>
    private Task _battleTask;
    /// <summary>
    /// 战斗任务取消操作句柄
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource;
    /// <summary>
    /// 客户端与服务器的估算时间差
    /// </summary>
    private long _timeOffset;
    /// <summary>
    /// 最后一次执行Ping的客户端时间
    /// </summary>
    private long _lastHeartbeatTime;
    /// <summary>
    /// 最后一次执行帧数据的客户端时间
    /// </summary>
    private long _lastProcessFrameTime;
    /// <summary>
    /// 下次发送帧数据时的帧号
    /// </summary>
    private uint _willSentFrame;
    /// <summary>
    /// 最近一次发送的帧数据
    /// </summary>
    private int _lastSentFrameData;
    /// <summary>
    /// 客户端计时器
    /// </summary>
    private readonly Stopwatch _stopwatch;
    /// <summary>
    /// 预测实体队列
    /// </summary>
    private Queue<BattleEntity> _predictEntityQueue;
    /// <summary>
    /// 预测帧数据队列
    /// </summary>
    private Queue<FrameBuffer.Frame> _predictFrameQueue;
    /// <summary>
    /// 服务器返回的帧数据队列
    /// </summary>
    private Queue<FrameBuffer.Frame> _serverFrameQueue;
    /// <summary>
    /// 最后一个服务器返回的帧数据
    /// </summary>
    private FrameBuffer.Frame _lastNetworkFrame;
    
    private BattleEntity _displayEntity;
    /// <summary>
    /// 用于显示的实体对象
    /// </summary>
    public BattleEntity DisplayEntity => _displayEntity;
    private BattleEntity _predictEntity;
    /// <summary>
    /// 预测的实体对象
    /// </summary>
    public BattleEntity PredictEntity => _predictEntity;
    private BattleEntity _confirmEntity;
    /// <summary>
    /// 确定的实体对象
    /// </summary>
    public BattleEntity ConfirmEntity => _confirmEntity;
    /// <summary>
    /// 实体池
    /// </summary>
    private BattleEntityPool _battleEntityPool;
    
    public BattleController()
    {
        _stopwatch = new Stopwatch();

        _predictEntityQueue = new Queue<BattleEntity>();
        _predictFrameQueue = new Queue<FrameBuffer.Frame>();
        _serverFrameQueue = new Queue<FrameBuffer.Frame>();

        _battleEntityPool = new BattleEntityPool();
    }

    public void InitEntities()
    {
        _confirmEntity = new BattleEntity();
        _predictEntity = new BattleEntity();
        _displayEntity = new BattleEntity();
        if (GameManager.Instance.TryGetRoom(GameManager.Instance.GetPlayer().RoomId, out var room))
        {
            for (int i = 0; i < room.PlayerIds.Count; i++)
                _confirmEntity.Players.Add(new PlayerEntity { ID = i });
        }
        _confirmEntity.CopyTo(_predictEntity);
        _confirmEntity.CopyTo(_displayEntity);
    }

    /// <summary>
    /// 开始客户端计时
    /// </summary>
    public void StartClientStopwatch()
    {
        _stopwatch.Start();
    }

    /// <summary>
    /// 获取客户端时间
    /// </summary>
    /// <returns>客户端流逝的时间</returns>
    public long GetClientStopwatchElapsedMilliseconds()
    {
        return _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 拷贝操作数据到玩家实体中
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="inputFrame"></param>
    private void CopyInput(BattleEntity entity, ref FrameBuffer.Frame inputFrame)
    {
        var playerList = entity.Players;
        for (int i = 0; i < playerList.Count; i++)
        {
            var input = new FrameBuffer.Input();
            if (inputFrame.GetInputByPos(playerList[i].ID, ref input))
            {
                playerList[i].Input.yaw = input.yaw;
                playerList[i].Input.key = input.key;
            }
        }
    }

    /// <summary>
    /// 战斗网络轮询
    /// </summary>
    /// <param name="cancellationToken">取消操作句柄</param>
    /// <returns>异步句柄</returns>
    public Task NetUpdate(CancellationToken cancellationToken)
    {
        var predictClientFrame = _predictEntity.Frame + 1;
        var predictClientFrameInput = GameManager.Instance.Input[predictClientFrame];
        if (_willSentFrame != default && predictClientFrameInput != default)
        {
            var input = new FrameBuffer.Input
            {
                pos = (byte)GameManager.Instance.GetRoomPos(),
                yaw = 0,
                key = predictClientFrameInput
            };
            Logger.Log(LogLevel.Info, $"NetUpdate SendFrame Frame:{_willSentFrame} Input:{input}");
            GameManager.Instance.SendFrame(_willSentFrame, input);
        }

        if (_stopwatch.ElapsedMilliseconds - _lastHeartbeatTime >= BattleSetting.HeartbeatTime)
        {
            _lastHeartbeatTime = _stopwatch.ElapsedMilliseconds;
            GameManager.Instance.Heartbeat();
        }
        
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }
    
    /// <summary>
    /// 战斗线程轮询
    /// </summary>
    /// <param name="cancellationToken">取消操作句柄</param>
    /// <returns>异步任务</returns>
    public Task LogicUpdate(CancellationToken cancellationToken)
    {
        RollbackConfirmAndSyncEntities(ref _lastNetworkFrame);
        
        var predictClientFrame = _predictEntity.Frame + 1;
        if (predictClientFrame - GameManager.Instance.ServerAuthorityFrame < 4)
        {
            // 估算服务器时间
            var estimateServerTime = _stopwatch.ElapsedMilliseconds - _timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = estimateServerTime + NetworkManager.Instance.RealPing * 0.5f >= predictClientFrame * BattleSetting.BattleInterval;
            var slowdown = _stopwatch.ElapsedMilliseconds - _lastProcessFrameTime >= BattleSetting.BattleInterval * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! predictClientFrame:{predictClientFrame} CurServerFrame:{GameManager.Instance.ServerAuthorityFrame}");
            if (hasNewFrame || slowdown)
            {
                _willSentFrame = (uint)predictClientFrame + 1;
                _lastProcessFrameTime = _stopwatch.ElapsedMilliseconds;
                
                EnqueueEntityToPredictQueueAndDisplay();
            }
        }
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    /// <summary>
    /// 比较帧数据是否相同
    /// </summary>
    /// <param name="networkFrame">服务器返回的输入</param>
    /// <param name="predictFrame">客户端预测的输入</param>
    /// <returns></returns>
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
    /// 预测执行
    /// </summary>
    private void EnqueueEntityToPredictQueueAndDisplay()
    {
        var newEntity = _battleEntityPool.Dequeue();
        // 加入预测实体队列
        _predictEntity.Frame++;
        _lastNetworkFrame.frame = _predictEntity.Frame;
        Logger.Log(LogLevel.Info, $"PredictiveExecute _lastNetworkFrame:{_lastNetworkFrame.frame} _predictEntity:{_predictEntity} _confirmEntity:{_confirmEntity}");

        _predictEntity.CopyTo(newEntity);
        _predictEntityQueue.Enqueue(newEntity);
        _predictFrameQueue.Enqueue(_lastNetworkFrame);
        
        // 交由显示实体用于显示
        newEntity.CopyTo(_displayEntity);
    }

    /// <summary>
    /// 拿到服务器返回的帧数据到客户端确定帧的帧数据集合
    /// </summary>
    private void UpdateServerFrameQueue()
    {
        _serverFrameQueue.Clear();
        var inputFrame = FrameBuffer.Frame.defFrame;
        for (var i = 0; i < BattleSetting.MaxPredictFrameCount; i++)
        {
            var frame = _confirmEntity.Frame + i + 1;
            if (frame > GameManager.Instance.ServerAuthorityFrame + 1) 
                break;
            if (GameManager.Instance.FrameBuffer.TryGetFrame(frame, ref inputFrame))
            {
                _serverFrameQueue.Enqueue(inputFrame);
            }
        }
    }

    /// <summary>
    /// 计算客户端与服务器的估算时间差
    /// </summary>
    private void CalculateDiffBetweenServerAndClientTime()
    {
        var timeOffsetShouldBe = _stopwatch.ElapsedMilliseconds - GameManager.Instance.ServerAuthorityFrame * BattleSetting.BattleInterval;
        if (timeOffsetShouldBe < _timeOffset) 
            _timeOffset = timeOffsetShouldBe;
    }

    /// <summary>
    /// 从预测实体队列中更新确认实体
    /// </summary>
    /// <param name="confirmPredictEntity">确认实体</param>
    /// <param name="lastServerFrame">最后一次服务器返回的帧数据</param>
    /// <returns>标记</returns>
    private bool UpdateConfirmPredictEntity(ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        var flag = false;
        while (_serverFrameQueue.Count > 0)
        {
            CalculateDiffBetweenServerAndClientTime();
            
            lastServerFrame = _serverFrameQueue.Dequeue();
            if (_predictEntityQueue.Count == 0) flag = true;
            if (_predictEntityQueue.Count > 0)
            {
                var predictFrame = _predictFrameQueue.Dequeue();
                //如果帧数据相同，直接使用预测实体
                if (CompareFrame(ref predictFrame, ref lastServerFrame))
                {
                    confirmPredictEntity = _predictEntityQueue.Dequeue();
                }
                else
                {
                    _battleEntityPool.Enqueue(_predictEntityQueue.Dequeue());
                    flag = true;
                }
            }

            if (!flag) continue;
            
            confirmPredictEntity.Frame++;
            CopyInput(confirmPredictEntity, ref lastServerFrame);
        }
        return flag;
    }

    /// <summary>
    /// 同步剩余的预测实体与确认实体
    /// </summary>
    /// <param name="flag">标记</param>
    /// <param name="confirmPredictEntity">确认实体</param>
    private void SyncConfirmPredictEntity(bool flag, ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        // 如果与确定实体不同，拷贝给确定实体
        if (confirmPredictEntity != _confirmEntity)
        {
            confirmPredictEntity.CopyTo(_confirmEntity);
            confirmPredictEntity = _confirmEntity;
        }
        
        Logger.Log(LogLevel.Info, $"SyncConfirmPredictEntity confirmPredictEntity:{confirmPredictEntity} _predictEntity:{_predictEntity}");
        
        if (flag)
        {
            // 如果预测队列中还存在有预测实体，则按最新的网络帧数据重新赋值
            confirmPredictEntity.CopyTo(_predictEntity);
            
            var count = _predictFrameQueue.Count;
            for (var i = 0; i < count; i++)
            {
                var predictInputFrame = _predictFrameQueue.Dequeue();
                for (int j = 0; j < predictInputFrame.Length; j++)
                {
                    var input = new FrameBuffer.Input();
                    if (lastServerFrame.GetInputByPos(predictInputFrame[j].pos, ref input))
                    {
                        predictInputFrame.SetInputByPos(input.pos, input);
                    }
                }
                _predictFrameQueue.Enqueue(predictInputFrame);

                var entity = _predictEntityQueue.Dequeue();
                _predictEntity.Frame++;
                CopyInput(_predictEntity, ref predictInputFrame);
                _predictEntity.CopyTo(entity);
                _predictEntityQueue.Enqueue(entity);
                
                if (i == count - 1) _displayEntity = entity;
            }
        }
        Logger.Log(LogLevel.Info, $"SyncConfirmPredictEntity _predictEntityQueue.Count:{_predictEntityQueue.Count} _confirmEntity:{_confirmEntity} _predictEntity:{_predictEntity}");
    }
    
    /// <summary>
    /// 预测回滚与同步实体
    /// </summary>
    /// <param name="lastServerFrame">最后一次服务器返回的帧数据</param>
    private void RollbackConfirmAndSyncEntities(ref FrameBuffer.Frame lastServerFrame)
    {
        UpdateServerFrameQueue();
        Logger.Log(LogLevel.Info, $"RollbackConfirmAndSyncEntities _serverFrameQueue.Count:{_serverFrameQueue.Count} _predictEntityQueue.Count:{_predictEntityQueue.Count} _confirmEntity:{_confirmEntity}");

        var confirmPredictEntity = _confirmEntity;
        var flag = UpdateConfirmPredictEntity(ref confirmPredictEntity, ref lastServerFrame);
        Logger.Log(LogLevel.Info, $"RollbackConfirmAndSyncEntities _serverFrameQueue.Count:{_serverFrameQueue.Count} lastServerFrame.D0:[{lastServerFrame[0]}] lastServerFrame.D1:[{lastServerFrame[1]}] confirmPredictEntity:{confirmPredictEntity} flag:{flag}");

        SyncConfirmPredictEntity(flag, ref confirmPredictEntity, ref lastServerFrame);
    }

}