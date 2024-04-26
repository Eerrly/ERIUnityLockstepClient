using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class BattleController
{
    private long _timeOffset;
    private readonly Stopwatch _stopwatch;
    private readonly Queue<BattleEntity> _predictBattleEntityQueue;
    private readonly Queue<FrameBuffer.Frame> _predictFrameQueue;
    private readonly Queue<FrameBuffer.Frame> _serverFrameQueue;
    private readonly BattleEntityPool _battleEntityPool;
    private uint _willSentFrame;
    private uint _lastSentFrame;
    private FrameBuffer.Input _lastSentInput;
    private FrameBuffer.Frame _lastNetworkFrame;
    private long _lastProcessFrameTime;
    private long _lastHeartbeatTime;

    private BattleEntity _displayBattleEntity;
    private BattleEntity _predictBattleEntity;
    private BattleEntity _confirmBattleEntity;

    public BattleEntity DisplayBattleEntity => _displayBattleEntity;
    public BattleEntity PredictBattleEntity => _predictBattleEntity;
    public BattleEntity ConfirmBattleEntity => _confirmBattleEntity;

    private int _lostFrameCount;
    private int _lastLostFrame;

    public BattleController()
    {
        _stopwatch = new Stopwatch();
        _battleEntityPool = new BattleEntityPool();
        _predictBattleEntityQueue = new Queue<BattleEntity>();
        _predictFrameQueue = new Queue<FrameBuffer.Frame>();
        _serverFrameQueue = new Queue<FrameBuffer.Frame>();
    }

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

    public Task LogicUpdate(CancellationToken cancellationToken)
    {
        RollbackConfirmAndSyncEntities(ref _lastNetworkFrame);
        
        var predictBattleClientFrame = _predictBattleEntity.Frame + 1;
        if (predictBattleClientFrame - GameManager.Instance.ServerAuthorityFrame < BattleSetting.MaxPredictFrameCount)
        {
            var magic = Math.Min(BattleSetting.BattleInterval, _lostFrameCount * 2 + 4);
            var estimateServerTime = _stopwatch.ElapsedMilliseconds - _timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = estimateServerTime + NetworkManager.Instance.RealPing * 0.5f + magic >= predictBattleClientFrame * BattleSetting.BattleInterval;
            var slowdown = _stopwatch.ElapsedMilliseconds - _lastProcessFrameTime >= BattleSetting.BattleInterval * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! predictBattleClientFrame:{predictBattleClientFrame} CurServerFrame:{GameManager.Instance.ServerAuthorityFrame}");
            if (hasNewFrame || slowdown){
                _willSentFrame = (uint)predictBattleClientFrame + 1;
                _lastProcessFrameTime = _stopwatch.ElapsedMilliseconds;
                EnqueueEntityToPredictQueueAndDisplay();
            }
        }

        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    private void RollbackConfirmAndSyncEntities(ref FrameBuffer.Frame lastServerFrame)
    {
        UpdateServerFrameQueue();

        var confirmPredictEntity = _confirmBattleEntity;
        var flag = UpdateConfirmPredictEntity(ref confirmPredictEntity, ref lastServerFrame);

        SyncConfirmPredictEntity(flag, ref confirmPredictEntity, ref lastServerFrame);
    }

    private bool UpdateConfirmPredictEntity(ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        var flag = false;
        while (_serverFrameQueue.Count > 0)
        {
            lastServerFrame = _serverFrameQueue.Dequeue();
            CalculateDiffBetweenServerAndClientTime(lastServerFrame.frame);

            var tmpInput = new FrameBuffer.Input();
            var tmpSelfInput = lastServerFrame.GetInputByPos(GameManager.Instance.GetBattlePos(), ref tmpInput);
            if (lastServerFrame.frame == _lastSentFrame && tmpSelfInput && !tmpInput.Compare(_lastSentInput))
            {
                _lostFrameCount ++;
                _lastLostFrame = lastServerFrame.frame;
            }
            
            if (_predictBattleEntityQueue.Count == 0) flag = true;
            if (_predictBattleEntityQueue.Count > 0)
            {
                var predictFrame = _predictFrameQueue.Dequeue();
                if (CompareFrame(ref lastServerFrame, ref predictFrame) && !flag)
                {
                    confirmPredictEntity = _predictBattleEntityQueue.Dequeue();
                }
                else
                {
                    _battleEntityPool.Enqueue(_predictBattleEntityQueue.Dequeue());
                    flag = true;
                }
            }
            if (flag)
            {
                UpdateEntityInput(confirmPredictEntity, ref lastServerFrame);
                confirmPredictEntity.Frame++;
                UpdateEntityState(confirmPredictEntity);
            }
            
            confirmPredictEntity.CopyTo(_confirmBattleEntity);
        }
        return flag;
    }

    private void SyncConfirmPredictEntity(bool flag, ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        if (confirmPredictEntity != _confirmBattleEntity)
        {
            confirmPredictEntity.CopyTo(_confirmBattleEntity);
            confirmPredictEntity = _confirmBattleEntity;
        }
        if (flag)
        {
            confirmPredictEntity.CopyTo(_predictBattleEntity);

            var count = _predictFrameQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var predictInputFrame = _predictFrameQueue.Dequeue();
                for (int j = 0; j < predictInputFrame.Length; j++)
                {
                    var input = new FrameBuffer.Input();
                    if (lastServerFrame.GetInputByPos(predictInputFrame[j].pos, ref input))
                        predictInputFrame.SetInputByPos(input.pos, input);
                }
                _predictFrameQueue.Enqueue(predictInputFrame);

                var predictEntity = _predictBattleEntityQueue.Dequeue();
                _predictBattleEntity.Frame++;
                UpdateEntityInput(_predictBattleEntity, ref predictInputFrame);
                UpdateEntityState(_predictBattleEntity);
                _predictBattleEntity.CopyTo(predictEntity);
                _predictBattleEntityQueue.Enqueue(predictEntity);

                if(i == count -1) predictEntity.CopyTo(_displayBattleEntity);
            }
        }
    }

    private void UpdateServerFrameQueue()
    {
        _serverFrameQueue.Clear();
        var inputFrame = FrameBuffer.Frame.defFrame;
        var frame = _confirmBattleEntity.Frame;
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

    private void EnqueueEntityToPredictQueueAndDisplay()
    {
        var newEntity = _battleEntityPool.Dequeue();

        _predictBattleEntity.Frame++;
        _lastNetworkFrame.frame = _predictBattleEntity.Frame;
        if (_lastSentInput.ToByte() != byte.MaxValue)
            _lastNetworkFrame.SetInputByPos(_lastSentInput.pos, _lastSentInput);
        UpdateEntityInput(_predictBattleEntity, ref _lastNetworkFrame);
        UpdateEntityState(_predictBattleEntity);

        newEntity.Name = "Predict";
        _predictBattleEntity.CopyTo(newEntity);
        _predictBattleEntityQueue.Enqueue(newEntity);
        _predictFrameQueue.Enqueue(_lastNetworkFrame);

        newEntity.CopyTo(_displayBattleEntity);
    }

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

    private void UpdateEntityState(BattleEntity battleEntity)
    {
        var playerEntities = battleEntity.PlayerEntities;
        foreach (var entity in playerEntities) PlayerStateMachine.Instance.Update(entity, battleEntity);
        foreach (var entity in playerEntities) PlayerStateMachine.Instance.LateUpdate(entity, battleEntity);
        foreach (var entity in playerEntities) PlayerStateMachine.Instance.DoChangeState(entity, battleEntity);
        foreach (var entity in playerEntities) MoveSystem.TransformLogicUpdate(entity, FixedNumber.MakeFixNum(BattleSetting.BattleInterval, 1000));
        
        Logger.Log(LogLevel.Info, $"UpdateEntityState lastNetworkFrame:{_lastNetworkFrame} battleEntity:{battleEntity}");
    }
    

}