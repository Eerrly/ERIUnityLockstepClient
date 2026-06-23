/// <summary>
/// 管理战斗、回放、重连的运行时对象。Phase 1 先承接对象持有和只读访问。
/// </summary>
public class BattleSessionController
{
    public FrameBuffer FrameBuffer { get; }
    public FrameEngine FrameEngine { get; }
    public BattleController BattleController { get; }
    public ReplayController ReplayController { get; }

    public BattleView BattleView { get; private set; }

    public BattleSessionController()
    {
        BattleController = new BattleController();
        ReplayController = new ReplayController();
        FrameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
        FrameEngine = new FrameEngine();
        FrameEngine.RegisterNetUpdateListener(BattleController.NetUpdate);
        FrameEngine.RegisterFrameUpdateListener(BattleController.LogicUpdate);
        FrameEngine.RegisterReplayUpdateListener(ReplayController.ReplayUpdate);
    }

    public void BindBattleView(BattleView battleView)
    {
        BattleView = battleView;
    }

    public void Release()
    {
        FrameEngine.UnRegisterFrameUpdateListener();
        FrameEngine.UnRegisterNetUpdateListener();
        FrameEngine.UnRegisterReplayUpdateListener();
    }
}
