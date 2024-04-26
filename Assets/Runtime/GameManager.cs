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
    private BattleView battleView;
    
    public int GetBattlePos()
    {
        return (int)(PlayerId - GameSetting.DefaultPlayerIdBase - 1);
    }

    public override void Initialize()
    {
        battleController = new BattleController();
        frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
        frameEngine = new FrameEngine();
        frameEngine.RegisterNetUpdateListener(battleController.NetUpdate);
        frameEngine.RegisterFrameUpdateListener(battleController.LogicUpdate);
        
        battleView = Util.GetOrAddComponent<BattleView>(gameObject);
    }

    public void StartBattle()
    {
        battleController.InitEntities();
        Loom.QueueOnMainThread(() => { battleView.InitView(battleController.DisplayBattleEntity); });
        Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Initialize), false);
        frameEngine.StartNetEngine(BattleSetting.NetInterval);
        frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
    }

    public void RenderUpdate(float deltaTime)
    {
        try
        {
            battleView.RenderUpdate(battleController.DisplayBattleEntity, deltaTime);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[RENDER] Exception ->\n{ex.Message}\n{ex.StackTrace}");
        }
    }
    
    public void StopBattle()
    {
        frameEngine.StopEngine();
        battleView.OnRelease(battleController.DisplayBattleEntity);
        Util.InvokeAttributeCall(this, typeof(EntitySystemAttribute), false, typeof(EntitySystemAttribute.Release), false);
        NetworkManager.Instance.KcpShutdown();
        NetworkManager.Instance.TcpShutdown();
    }

}