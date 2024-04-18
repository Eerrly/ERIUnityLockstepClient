using System.Diagnostics;

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
    private FrameBuffer.Frame lastNetworkFrame;
    private long lastProcessFrameTime;
    private long lastHeartbeatTime;

    private BattleEntity displayBattleEntity;
    private BattleEntity predictBattleEntity;
    private BattleEntity confirmBattleEntity;

    public BattleEntity DisplayBattleEntity => displayBattleEntity;
    public BattleEntity PredictBattleEntity => predictBattleEntity;
    public BattleEntity ConfirmBattleEntity => confirmBattleEntity;

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
        confirmBattleEntity = new BattleEntity();
        predictBattleEntity = new BattleEntity();
        displayBattleEntity = new BattleEntity();
        for (int i = 0; i < GameManager.Instance.RoomInfo.Gamers.Count; i ++)
            confirmBattleEntity.PlayerEntities.Add(new PlayerEntity(){ 
                ID = (int)(GameManager.Instance.RoomInfo.Gamers[i] - GameSetting.DefaultPlayerIdBase - 1) 
            });
        confirmBattleEntity.CopyTo(predictBattleEntity);
        confirmBattleEntity.CopyTo(displayBattleEntity);
    }

    public void StartClientStopwatch()
    {
        stopwatch.Start();
    }

    private void CopyInput(BattleEntity entity, ref FrameBuffer.Frame inputFrame)
    {
        var playerList = entity.PlayerEntities;
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

    public Task NetUpdate(CancellationToken cancellationToken)
    {
        var predictBattleClientFrame = predictBattleEntity.Frame + 1;
        var predictBattleClientFrameInput = GameManager.Instance.Input[predictBattleClientFrame];
        if (willSentFrame != default && predictBattleClientFrameInput != default && lastSentFrame != willSentFrame)
        {
            var input = new FrameBuffer.Input
            {
                pos = (byte)GameManager.Instance.GetBattlePos(),
                yaw = 0,
                key = predictBattleClientFrameInput,
            };
            System.Console.WriteLine($"NetUpdate SendFrame Frame:{willSentFrame} Input:{input}");
            NetworkManager.Instance.SendBattleFrameMessage(willSentFrame, input.ToByte());
            lastSentFrame = willSentFrame;
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
            var estimateServerTime = stopwatch.ElapsedMilliseconds - timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = estimateServerTime + NetworkManager.Instance.RealPing * 0.5f >= predictBattleClientFrame * BattleSetting.BattleInterval;
            var slowdown = stopwatch.ElapsedMilliseconds - lastProcessFrameTime >= BattleSetting.BattleInterval * 2;
            if (slowdown){
                System.Console.WriteLine($"Slowdown! predictBattleClientFrame:{predictBattleClientFrame} CurServerFrame:{GameManager.Instance.ServerAuthorityFrame}");
            }
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
        System.Console.WriteLine($"RollbackConfirmAndSyncEntities serverFrameQueue.Count:{serverFrameQueue.Count} predictBattleEntityQueue.Count:{predictBattleEntityQueue.Count} confirmBattleEntity:{confirmBattleEntity}");

        var confirmPredictEntity = confirmBattleEntity;
        var flag = UpdateConfirmPredictEntity(ref confirmPredictEntity, ref lastServerFrame);
        System.Console.WriteLine($"RollbackConfirmAndSyncEntities serverFrameQueue.Count:{serverFrameQueue.Count} lastServerFrame.D0:[{lastServerFrame[0]}] lastServerFrame.D1:[{lastServerFrame[1]}] confirmPredictEntity:{confirmPredictEntity} flag:{flag}");

        SyncConfirmPredictEntity(flag, ref confirmPredictEntity, ref lastServerFrame);
    }

    private bool UpdateConfirmPredictEntity(ref BattleEntity confirmPredictEntity, ref FrameBuffer.Frame lastServerFrame)
    {
        var flag = false;
        while (serverFrameQueue.Count > 0)
        {
            CalculateDiffBetweenServerAndClientTime();
            
            lastServerFrame = serverFrameQueue.Dequeue();
            if (predictBattleEntityQueue.Count == 0) flag = true;
            if (predictBattleEntityQueue.Count > 0)
            {
                var predictFrame = predictFrameQueue.Dequeue();
                if (CompareFrame(ref predictFrame, ref lastServerFrame))
                {
                    confirmPredictEntity = predictBattleEntityQueue.Dequeue();
                }
                else
                {
                    battleEntityPool.Enqueue(predictBattleEntityQueue.Dequeue());
                    flag = true;
                }
            }

            if (!flag) continue;
            
            confirmPredictEntity.Frame++;
            CopyInput(confirmPredictEntity, ref lastServerFrame);
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
        System.Console.WriteLine($"SyncConfirmPredictEntity confirmPredictEntity:{confirmPredictEntity} predictBattleEntity:{predictBattleEntity}");
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
                CopyInput(predictBattleEntity, ref predictInputFrame);
                predictBattleEntity.CopyTo(predictEntity);
                predictBattleEntityQueue.Enqueue(predictEntity);

                if(i == count -1) displayBattleEntity = predictEntity;
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
    }

    private void CalculateDiffBetweenServerAndClientTime()
    {
        var timeOffsetShouldBe = stopwatch.ElapsedMilliseconds - GameManager.Instance.ServerAuthorityFrame * BattleSetting.BattleInterval;
        if (timeOffsetShouldBe < timeOffset) 
            timeOffset = timeOffsetShouldBe;
        System.Console.WriteLine($"CalculateDiffBetweenServerAndClientTime CurServerFrame:{GameManager.Instance.ServerAuthorityFrame} timeOffsetShouldBe:{timeOffsetShouldBe}");
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
        System.Console.WriteLine($"EnqueueEntityToPredictQueueAndDisplay lastNetworkFrame:{lastNetworkFrame.frame} predictBattleEntity:{predictBattleEntity} confirmBattleEntity:{confirmBattleEntity}");
    
        predictBattleEntity.CopyTo(newEntity);
        predictBattleEntityQueue.Enqueue(newEntity);
        predictFrameQueue.Enqueue(lastNetworkFrame);

        newEntity.CopyTo(displayBattleEntity);
    }
    

}