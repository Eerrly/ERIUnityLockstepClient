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
                battleController.InitEntities();
                LoomManager.Instance.QueueOnMainThread(() => { battleView.InitView(battleController.DisplayBattleEntity); });
                Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Initialize), false);
                frameEngine.StartNetEngine(BattleSetting.NetInterval);
                frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
                LoomManager.Instance.QueueOnMainThread(() => { BattleRecordManager.Instance.StartRecordBattle(GetBattlePos()); });
                ReplaySystem.Init();
                break;
            }
            case BattleType.Replay:
            {
                replayController.InitReplay(GetBattlePos());
                LoomManager.Instance.QueueOnMainThread(() => { battleView.InitView(replayController.DisplayBattleEntity); });
                Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Initialize), false);
                frameEngine.StartReplayEngine(BattleSetting.BattleInterval);
                replayController.StartReplayStopwatch();
                break;
            }
        }
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
                frameEngine.StopEngine();
                battleView.OnRelease(battleController.DisplayBattleEntity);
                Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Release), false);
                NetworkManager.Instance.KcpShutdown();
                NetworkManager.Instance.TcpShutdown();
                ReplaySystem.Release();
                break;
            }
            case BattleType.Replay:
            {
                frameEngine.StopReplayEngine();
                battleView.OnRelease(replayController.DisplayBattleEntity);
                Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Release), false);
                break;
            }
        }
        
    }

}