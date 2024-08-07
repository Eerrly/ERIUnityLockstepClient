using System;

public class GameManager : MManager<GameManager>
{
    public uint PlayerId;
    public RoomInfo RoomInfo;
    public int ServerAuthorityFrame = -1;
    public bool IsBattleConnected = false;
    public bool IsBattleStart = false;

    private FrameBuffer _frameBuffer;
    public FrameBuffer FrameBuffer
    {
        get => _frameBuffer;
        set => _frameBuffer = value;
    }
    private FrameEngine _frameEngine;
    private BattleController _battleController;
    private ReplayController _replayController;
    private BattleView _battleView;
    
    public int GetBattlePos()
    {
        return (int)(PlayerId - GameSetting.DefaultPlayerIdBase - 1);
    }

    public override void Initialize()
    {
        _battleController = new BattleController();
        _replayController = new ReplayController();
        _frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
        _frameEngine = new FrameEngine();
        _frameEngine.RegisterNetUpdateListener(_battleController.NetUpdate);
        _frameEngine.RegisterFrameUpdateListener(_battleController.LogicUpdate);
        _frameEngine.RegisterReplayUpdateListener(_replayController.ReplayUpdate);
        
        _battleView = Util.GetOrAddComponent<BattleView>(gameObject);
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
        _battleController.InitEntities();
        LoomManager.Instance.QueueOnMainThread(() => { _battleView.InitView(_battleController.DisplayBattleEntity); });
        InitializeEntitySystems();
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
        LoomManager.Instance.QueueOnMainThread(() => { BattleRecordManager.Instance.StartRecordBattle(GetBattlePos()); });
        ReplaySystem.Init();
    }

    private void StartReplayBattle()
    {
        _replayController.InitReplay(GetBattlePos());
        LoomManager.Instance.QueueOnMainThread(() => { _battleView.InitView(_replayController.DisplayBattleEntity); });
        InitializeEntitySystems();
        _frameEngine.StartReplayEngine(BattleSetting.BattleInterval);
        _replayController.StartReplayStopwatch();
    }

    private void InitializeEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Initialize), false);
    }

    public void RenderUpdate(BattleType battleType, float deltaTime)
    {
        try
        {
            switch (battleType)
            {
                case BattleType.Remote:
                {
                    _battleView.RenderUpdate(_battleController.DisplayBattleEntity, deltaTime);
                    break;
                }
                case BattleType.Replay:
                {
                    _battleView.RenderUpdate(_replayController.DisplayBattleEntity, deltaTime);
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
        _frameEngine.StopEngine();
        _battleView.OnRelease(_battleController.DisplayBattleEntity);
        ReleaseEntitySystems();
        NetworkManager.Instance.KcpShutdown();
        ReplaySystem.Release();
    }

    private void StopReplayBattle()
    {
        _frameEngine.StopReplayEngine();
        _battleView.OnRelease(_replayController.DisplayBattleEntity);
        ReleaseEntitySystems();
    }

    private void ReleaseEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Release), false);
    }
    
}