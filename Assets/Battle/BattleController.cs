using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class BattleController
{
    private long timeOffset;
    private readonly Stopwatch stopwatch;
    private Queue<BattleEntity> predictBattleEntityQueue;
    private Queue<FrameBuffer.Frame> predictFrameQueue;
    private Queue<FrameBuffer.Frame> serverFrameQueue;
    private BattleEntityPool battleEntityPool;
    private uint willSentFrame;
    private uint lastSentFrame;
    private FrameBuffer.Input lastSentInput;
    private FrameBuffer.Frame lastNetworkFrame;
    private long lastProcessFrameTime;
    private long lastHeartbeatTime;

    private BattleEntity displayBattleEntity;
    private BattleEntity predictBattleEntity;
    private BattleEntity confirmBattleEntity;

    public BattleEntity DisplayBattleEntity => displayBattleEntity;
    public BattleEntity PredictBattleEntity => predictBattleEntity;
    public BattleEntity ConfirmBattleEntity => confirmBattleEntity;

    private int lostFrameCount;
    private int lastLostFrame;

    public BattleController()
    {
        stopwatch = new Stopwatch();
        battleEntityPool = new BattleEntityPool();
        predictBattleEntityQueue = new Queue<BattleEntity>();
        predictFrameQueue = new Queue<FrameBuffer.Frame>();
        serverFrameQueue = new Queue<FrameBuffer.Frame>();
    }

    public void InitEntities()
    {
        confirmBattleEntity = new BattleEntity(); confirmBattleEntity.Init();
        confirmBattleEntity.Name = "Confirm";
        predictBattleEntity = new BattleEntity(); predictBattleEntity.Init();
        predictBattleEntity.Name = "Predict";
        displayBattleEntity = new BattleEntity(); displayBattleEntity.Init();
        displayBattleEntity.Name = "Display";
        for (var i = 0; i < GameManager.Instance.RoomInfo.Gamers.Count; i++)
        {
            var playerEntity = new PlayerEntity(); 
            playerEntity.Init();
            playerEntity.ID = (int)(GameManager.Instance.RoomInfo.Gamers[i] - GameSetting.DefaultPlayerIdBase - 1);
            confirmBattleEntity.PlayerEntities.Add(playerEntity);
        }
        confirmBattleEntity.CopyTo(predictBattleEntity);
        confirmBattleEntity.CopyTo(displayBattleEntity);
    }

    public Task NetUpdate(CancellationToken cancellationToken)
    {
        var input = InputManager.Instance.GetInput(GameManager.Instance.GetBattlePos());
        if (willSentFrame != default && !input.Compare(lastSentInput) && lastSentFrame != willSentFrame)
        {
            Logger.Log(LogLevel.Info, $"NetUpdate SendFrame Frame:{willSentFrame} Input:{input}");
            NetworkManager.Instance.SendBattleFrameMessage(willSentFrame, input.ToByte());
            lastSentFrame = willSentFrame;
            lastSentInput = input;
        }
        if (stopwatch.ElapsedMilliseconds - lastHeartbeatTime >= BattleSetting.HeartbeatTime)
        {
            lastHeartbeatTime = stopwatch.ElapsedMilliseconds;
            NetworkManager.Instance.SendBattleHeartBeatMessage(GameManager.Instance.PlayerId, (ulong)stopwatch.ElapsedMilliseconds);
        }
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    public Task LogicUpdate(CancellationToken cancellationToken)
    {
        RollbackConfirmAndSyncEntities(ref lastNetworkFrame);
        
        var predictBattleClientFrame = predictBattleEntity.Frame + 1;
        if (predictBattleClientFrame - GameManager.Instance.ServerAuthorityFrame < BattleSetting.MaxPredictFrameCount)
        {
            var magic = Math.Min(BattleSetting.BattleInterval, lostFrameCount * 2 + 4);
            var estimateServerTime = stopwatch.ElapsedMilliseconds - timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = estimateServerTime + NetworkManager.Instance.RealPing * 0.5f + magic >= predictBattleClientFrame * BattleSetting.BattleInterval;
            var slowdown = stopwatch.ElapsedMilliseconds - lastProcessFrameTime >= BattleSetting.BattleInterval * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! predictBattleClientFrame:{predictBattleClientFrame} CurServerFrame:{GameManager.Instance.ServerAuthorityFrame}");
            if (hasNewFrame || slowdown){
                willSentFrame = (uint)predictBattleClientFrame + 1;
                lastProcessFrameTime = stopwatch.ElapsedMilliseconds;
                EnqueueEntityToPredictQueueAndDisplay();
            }
        }

        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    private void RollbackConfirmAndSyncEntities(ref FrameBuffer.Frame lastServerFrame)
    {
        UpdateServerFrameQueue();

        var confirmPredictEntity = confirmBattleEntity;
        var flag = UpdateConfirmPredictEntity(ref confirmPredictEntity, ref lastServerFrame);

        SyncConfirmPredictEntity(flag, ref confirmPredictEntity, ref lastServerFrame);
    }

    private bool UpdateConfirmPredictEntity(ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        var flag = false;
        while (serverFrameQueue.Count > 0)
        {
            lastServerFrame = serverFrameQueue.Dequeue();
            CalculateDiffBetweenServerAndClientTime(lastServerFrame.frame);

            var tmpInput = new FrameBuffer.Input();
            var tmpSelfInput = lastServerFrame.GetInputByPos(GameManager.Instance.GetBattlePos(), ref tmpInput);
            if (lastServerFrame.frame == lastSentFrame && tmpSelfInput && !tmpInput.Compare(lastSentInput))
            {
                lostFrameCount ++;
                lastLostFrame = lastServerFrame.frame;
            }
            
            if (predictBattleEntityQueue.Count == 0) flag = true;
            if (predictBattleEntityQueue.Count > 0)
            {
                var predictFrame = predictFrameQueue.Dequeue();
                if (CompareFrame(ref lastServerFrame, ref predictFrame) && !flag)
                {
                    confirmPredictEntity = predictBattleEntityQueue.Dequeue();
                }
                else
                {
                    battleEntityPool.Enqueue(predictBattleEntityQueue.Dequeue());
                    flag = true;
                }
            }
            if (flag)
            {
                UpdateEntityInput(confirmPredictEntity, ref lastServerFrame);
                confirmPredictEntity.Frame++;
                UpdateEntityState(confirmPredictEntity);
            }
            
            confirmPredictEntity.CopyTo(confirmBattleEntity);
        }
        return flag;
    }

    private void SyncConfirmPredictEntity(bool flag, ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        if (confirmPredictEntity != confirmBattleEntity)
        {
            confirmPredictEntity.CopyTo(confirmBattleEntity);
            confirmPredictEntity = confirmBattleEntity;
        }
        if (flag)
        {
            confirmPredictEntity.CopyTo(predictBattleEntity);

            var count = predictFrameQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var predictInputFrame = predictFrameQueue.Dequeue();
                for (int j = 0; j < predictInputFrame.Length; j++)
                {
                    var input = new FrameBuffer.Input();
                    if (lastServerFrame.GetInputByPos(predictInputFrame[j].pos, ref input))
                        predictInputFrame.SetInputByPos(input.pos, input);
                }
                predictFrameQueue.Enqueue(predictInputFrame);

                var predictEntity = predictBattleEntityQueue.Dequeue();
                predictBattleEntity.Frame++;
                UpdateEntityInput(predictBattleEntity, ref predictInputFrame);
                UpdateEntityState(predictBattleEntity);
                predictBattleEntity.CopyTo(predictEntity);
                predictBattleEntityQueue.Enqueue(predictEntity);

                if(i == count -1) predictEntity.CopyTo(displayBattleEntity);
            }
        }
    }

    private void UpdateServerFrameQueue()
    {
        serverFrameQueue.Clear();
        var inputFrame = FrameBuffer.Frame.defFrame;
        var frame = confirmBattleEntity.Frame;
        for (int i = 0; i < BattleSetting.MaxPredictFrameCount; i++)
        {
            if(frame++ > GameManager.Instance.ServerAuthorityFrame)
                break;
            if(!GameManager.Instance.FrameBuffer.TryGetFrame(frame, ref inputFrame))
                break;
            serverFrameQueue.Enqueue(inputFrame);
        }

        if (GameManager.Instance.FrameBuffer.LastSetFrameIndex - frame > BattleSetting.MaxPredictFrameCount)
            lostFrameCount = 0;
        if (lostFrameCount > 0 && frame - lastLostFrame >= BattleSetting.BattleInterval * 2)
            lostFrameCount --;
    }

    private void CalculateDiffBetweenServerAndClientTime(int frame)
    {
        if (frame == 0) 
        {
            stopwatch.Start();
            timeOffset = 0;
        }
        var timeOffsetShouldBe = stopwatch.ElapsedMilliseconds - GameManager.Instance.ServerAuthorityFrame * BattleSetting.BattleInterval;
        if (timeOffsetShouldBe < timeOffset) 
            timeOffset = timeOffsetShouldBe;
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
        var newEntity = battleEntityPool.Dequeue();

        predictBattleEntity.Frame++;
        lastNetworkFrame.frame = predictBattleEntity.Frame;
        lastNetworkFrame.SetInputByPos(lastSentInput.pos, lastSentInput);
        UpdateEntityInput(predictBattleEntity, ref lastNetworkFrame);
        UpdateEntityState(predictBattleEntity);

        newEntity.Name = "Predict";
        predictBattleEntity.CopyTo(newEntity);
        predictBattleEntityQueue.Enqueue(newEntity);
        predictFrameQueue.Enqueue(lastNetworkFrame);

        newEntity.CopyTo(displayBattleEntity);
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
    }
    

}