using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器
/// </summary>
public class GameManager : MManager<GameManager>
{
    public event Action OnReplayFinished;
    public event Action<string> OnStatusMessage;

    public uint PlayerId;
    public RoomInfo RoomInfo;
    public int ServerAuthorityFrame = -1;
    public bool IsBattleConnected = false;
    public bool IsBattleStart = false;
    public BattleType CurrentBattleType { get; private set; } = BattleType.Remote;

    private FrameBuffer _frameBuffer;
    public FrameBuffer FrameBuffer
    {
        get => _frameBuffer;
        set => _frameBuffer = value;
    }

    public bool IsReplayPaused => _replayController != null && _replayController.IsPaused;
    public float ReplayPlaybackSpeed => _replayController == null ? 1f : _replayController.PlaybackSpeed;
    public bool IsReplayFinished => _replayController != null && _replayController.IsReplayFinished;

    private FrameEngine _frameEngine;
    private BattleController _battleController;
    private ReplayController _replayController;
    private BattleView _battleView;
    private bool _battleViewInitialized;
    private Coroutine _remoteExitCoroutine;
    private Coroutine _replayExitCoroutine;

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
        CurrentBattleType = battleType;
        IsBattleStart = true;

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
        CameraManager.Instance.ApplyBattleCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Battle);

        if (_battleView == null)
            CreateBattleView();

        _battleController.InitEntities();
        _battleView.InitView(_battleController.DisplayBattleEntity);
        _battleViewInitialized = true;
        InitializeEntitySystems();
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
        BattleRecordManager.Instance.StartRecordBattle(GetBattlePos());
    }

    private void StartReplayBattle()
    {
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            CreateBattleView();
            CameraManager.Instance.ApplyReplayCameraState();

            _replayController.InitReplay(GetBattlePos());
            _replayController.RestartReplay();
            UIRootManager.Instance.ShowRoot(UIRootType.Replay);
            _battleView.InitView(_replayController.DisplayBattleEntity);
            _battleViewInitialized = true;
            InitializeEntitySystems();
            _frameEngine.StartReplayEngine(BattleSetting.BattleInterval);
        }));
    }

    private void InitializeEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Initialize), false);
    }

    public void RenderUpdate(float deltaTime)
    {
        if (!IsBattleStart || _battleView == null)
            return;

        try
        {
            switch (CurrentBattleType)
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

    public void StopBattle()
    {
        StopBattle(CurrentBattleType);
    }

    private void StopRemoteBattle()
    {
        if (!IsBattleStart && _battleView == null)
            return;

        _frameEngine?.StopEngine();
        if (_battleView != null)
        {
            if (_battleViewInitialized)
                _battleView.OnRelease(_battleController.DisplayBattleEntity);
            Destroy(_battleView.gameObject);
            _battleView = null;
            _battleViewInitialized = false;
        }

        ReleaseEntitySystems();
        BattleRecordManager.Instance.OnRelease();
        NetworkManager.Instance.KcpShutdown();
        IsBattleStart = false;
    }

    private void StopReplayBattle()
    {
        _frameEngine?.StopReplayEngine();
        if (_battleView != null)
        {
            if (_battleViewInitialized)
                _battleView.OnRelease(_replayController.DisplayBattleEntity);
            Destroy(_battleView.gameObject);
            _battleView = null;
            _battleViewInitialized = false;
        }

        ReleaseEntitySystems();
        IsBattleStart = false;
    }

    public void SetReplayPaused(bool isPaused)
    {
        if (_replayController == null)
            return;

        _replayController.SetPaused(isPaused);
    }

    public void SetReplayPlaybackSpeed(float playbackSpeed)
    {
        if (_replayController == null)
            return;

        _replayController.SetPlaybackSpeed(playbackSpeed);
    }

    public void RestartReplay()
    {
        if (_replayController == null)
            return;

        _replayController.RestartReplay();
        IsBattleStart = true;
        OnStatusMessage?.Invoke("回放已重播");
    }

    public void ExitReplayToMain()
    {
        if (_replayExitCoroutine != null)
            StopCoroutine(_replayExitCoroutine);
        _replayExitCoroutine = StartCoroutine(ExitReplayToMainRoutine());
    }

    public void HandleRemoteBattleExit(string reason)
    {
        if (_remoteExitCoroutine != null)
            StopCoroutine(_remoteExitCoroutine);
        _remoteExitCoroutine = StartCoroutine(HandleRemoteBattleExitRoutine(reason));
    }

    private void HandleReplayFinished()
    {
        LoomManager.Instance.QueueOnMainThread(() =>
        {
            OnReplayFinished?.Invoke();
        });
    }

    private IEnumerator ExitReplayToMainRoutine()
    {
        StopReplayBattle();
        yield return SceneManager.LoadSceneAsync((int)EGameScene.Main, LoadSceneMode.Single);
        CameraManager.Instance.ApplyMainCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Modern);
        _replayExitCoroutine = null;
    }

    private IEnumerator HandleRemoteBattleExitRoutine(string reason)
    {
        StopRemoteBattle();
        yield return SceneManager.LoadSceneAsync((int)EGameScene.Main, LoadSceneMode.Single);
        CameraManager.Instance.ApplyMainCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Modern);
        OnStatusMessage?.Invoke(string.IsNullOrEmpty(reason) ? "战斗已退出" : reason);
        _remoteExitCoroutine = null;
    }

    private void CreateBattleView()
    {
        if (_battleView != null)
            Destroy(_battleView.gameObject);

        var go = new GameObject("BattleView");
        _battleView = Util.GetOrAddComponent<BattleView>(go);
        _battleViewInitialized = false;
    }

    public void OnRoomFull()
    {
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            CreateBattleView();
            CameraManager.Instance.ApplyBattleCameraState();
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
