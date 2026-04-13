using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器
/// </summary>
public class GameManager : MManager<GameManager>
{
    public event Action<string> OnReplayCompleted;

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
        Application.targetFrameRate = GameSetting.TargetFrameRate;

        _battleController = new BattleController();
        _replayController = new ReplayController();
        _replayController.OnReplayFinished += HandleReplayFinished;
        _frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
        _frameEngine = new FrameEngine();
        _frameEngine.RegisterNetUpdateListener(_battleController.NetUpdate);
        _frameEngine.RegisterFrameUpdateListener(_battleController.LogicUpdate);
        _frameEngine.RegisterReplayUpdateListener(_replayController.ReplayUpdate);
    }

    public void StartBattle(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
                StartRemoteBattle();
                break;
            case BattleType.Replay:
                StartReplayBattle();
                break;
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
    }

    private void StartReplayBattle()
    {
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            CreateBattleView();
            CameraManager.Instance.ToggleUICamera();
            CameraManager.Instance.ToggleBattleCamera();

            _replayController.InitReplay(GetBattlePos());
            _battleView.InitView(_replayController.DisplayBattleEntity);
            InitializeEntitySystems();
            _frameEngine.StartReplayEngine(BattleSetting.BattleInterval);
            _replayController.StartReplayStopwatch();
        }));
    }

    private void InitializeEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Initialize), false);
    }

    public void RenderUpdate(BattleType battleType, float deltaTime)
    {
        try
        {
            if (_battleView == null)
                return;

            switch (battleType)
            {
                case BattleType.Remote:
                    _battleView.RenderUpdate(_battleController.DisplayBattleEntity, deltaTime);
                    break;
                case BattleType.Replay:
                    _battleView.RenderUpdate(_replayController.DisplayBattleEntity, deltaTime);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"[RENDER] Exception ->\n{ex.Message}\n{ex.StackTrace}");
        }
    }

    public void StopBattle(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
                StopRemoteBattle();
                break;
            case BattleType.Replay:
                StopReplayBattle();
                break;
        }
    }

    private void StopRemoteBattle()
    {
        if (!IsBattleStart)
            return;

        _frameEngine?.StopEngine();
        if (_battleView != null)
        {
            _battleView.OnRelease(_battleController.DisplayBattleEntity);
            _battleView = null;
        }
        ReleaseEntitySystems();
        NetworkManager.Instance.KcpShutdown();
        IsBattleStart = false;
    }

    private void StopReplayBattle()
    {
        _frameEngine?.StopReplayEngine();
        if (_battleView != null)
        {
            _battleView.OnRelease(_replayController.DisplayBattleEntity);
            _battleView = null;
        }
        ReleaseEntitySystems();
        IsBattleStart = false;
    }

    private void HandleReplayFinished()
    {
        LoomManager.Instance.QueueOnMainThread(() =>
        {
            StartCoroutine(OnReplayFinishedRoutine());
        });
    }

    private IEnumerator OnReplayFinishedRoutine()
    {
        StopReplayBattle();
        yield return SceneManager.LoadSceneAsync((int)EGameScene.Main, LoadSceneMode.Single);
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ToggleBattleCamera();
            CameraManager.Instance.ToggleUICamera();
        }
        OnReplayCompleted?.Invoke("回放结束");
    }

    private void CreateBattleView()
    {
        var go = new GameObject("BattleView");
        _battleView = Util.GetOrAddComponent<BattleView>(go);
    }

    public void OnRoomFull()
    {
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            CreateBattleView();
            CameraManager.Instance.ToggleBattleCamera();
            NetworkManager.Instance.KcpConnect();
            NetworkManager.Instance.KcpUpdate();
        }));
    }

    private IEnumerator OnLoadBattleSceneAsync(Action callback)
    {
        yield return SceneManager.LoadSceneAsync((int)EGameScene.World, LoadSceneMode.Single);
        callback.Invoke();
    }

    private void ReleaseEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Release), false);
    }

    public override void OnRelease()
    {
        if (_frameEngine != null)
        {
            _frameEngine.UnRegisterFrameUpdateListener();
            _frameEngine.UnRegisterNetUpdateListener();
            _frameEngine.UnRegisterReplayUpdateListener();
        }

        if (_replayController != null)
            _replayController.OnReplayFinished -= HandleReplayFinished;
    }
}
