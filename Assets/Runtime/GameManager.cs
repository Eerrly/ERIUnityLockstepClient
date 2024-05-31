using System;

public class GameManager : MManager<GameManager>
{
    public uint PlayerId;
    public RoomInfo RoomInfo;
    public int ServerAuthorityFrame = -1;
    public bool IsBattleConnected = false;
    public bool IsBattleStart = false;

    private FrameBuffer frameBuffer;
    public FrameBuffer FrameBuffer
    {
        get => frameBuffer;
        set => frameBuffer = value;
    }
    private FrameEngine frameEngine;
    private BattleController battleController;
    private ReplayController replayController;
    private BattleView battleView;
    
    public int GetBattlePos()
    {
        return (int)(PlayerId - GameSetting.DefaultPlayerIdBase - 1);
    }

    public override void Initialize()
    {
        battleController = new BattleController();
        replayController = new ReplayController();
        frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
        frameEngine = new FrameEngine();
        frameEngine.RegisterNetUpdateListener(battleController.NetUpdate);
        frameEngine.RegisterFrameUpdateListener(battleController.LogicUpdate);
        frameEngine.RegisterReplayUpdateListener(replayController.ReplayUpdate);
        
        battleView = Util.GetOrAddComponent<BattleView>(gameObject);
    }

    public void StartBattle(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
            {
                StartRemoteBattle();
                break;
            }
            case BattleType.Replay:
            {
                StartReplayBattle();
                break;
            }
        }
    }

    private void StartRemoteBattle()
    {
        battleController.InitEntities();
        LoomManager.Instance.QueueOnMainThread(() => { battleView.InitView(battleController.DisplayBattleEntity); });
        InitializeEntitySystems();
        frameEngine.StartNetEngine(BattleSetting.NetInterval);
        frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
        LoomManager.Instance.QueueOnMainThread(() => { BattleRecordManager.Instance.StartRecordBattle(GetBattlePos()); });
        ReplaySystem.Init();
    }

    private void StartReplayBattle()
    {
        replayController.InitReplay(GetBattlePos());
        LoomManager.Instance.QueueOnMainThread(() => { battleView.InitView(replayController.DisplayBattleEntity); });
        InitializeEntitySystems();
        frameEngine.StartReplayEngine(BattleSetting.BattleInterval);
        replayController.StartReplayStopwatch();
    }

    private void InitializeEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Initialize), false);
    }

    public void RenderUpdate(BattleType battleType, float deltaTime)
    {
        try
        {
            switch (battleType)
            {
                case BattleType.Remote:
                {
                    battleView.RenderUpdate(battleController.DisplayBattleEntity, deltaTime);
                    break;
                }
                case BattleType.Replay:
                {
                    battleView.RenderUpdate(replayController.DisplayBattleEntity, deltaTime);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[RENDER] Exception ->\n{ex.Message}\n{ex.StackTrace}");
        }
    }
    
    public void StopBattle(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
            {
                StopRemoteBattle();
                break;
            }
            case BattleType.Replay:
            {
                StopReplayBattle();
                break;
            }
        }
    }

    private void StopRemoteBattle()
    {
        frameEngine.StopEngine();
        battleView.OnRelease(battleController.DisplayBattleEntity);
        ReleaseEntitySystems();
        NetworkManager.Instance.KcpShutdown();
        NetworkManager.Instance.TcpShutdown();
        ReplaySystem.Release();
    }

    private void StopReplayBattle()
    {
        frameEngine.StopReplayEngine();
        battleView.OnRelease(replayController.DisplayBattleEntity);
        ReleaseEntitySystems();
    }

    private void ReleaseEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Release), false);
    }
    
}