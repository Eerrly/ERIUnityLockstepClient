using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器
/// </summary>
public class GameManager : MManager<GameManager>
{
    private const float ReconnectTimeoutSeconds = 10f;

    public event Action OnReplayFinished;
    public event Action<string> OnStatusMessage;
    public event Action<ReconnectLoadingStatus, string> OnReconnectLoadingStatusChanged;

    public uint PlayerId;
    public RoomInfo RoomInfo;
    public int ServerAuthorityFrame = -1;
    public bool IsBattleConnected = false;
    public bool IsBattleStart = false;
    public bool IsReconnecting { get; private set; }
    public bool ShouldIgnoreReconnectDisconnectFailure => _suppressReconnectDisconnectFailure;
    public ReconnectLoadingStatus ReconnectStatus { get; private set; } = ReconnectLoadingStatus.None;
    public string ReconnectProgressText { get; private set; } = string.Empty;
    public float ReconnectProgress01 { get; private set; }
    public ReconnectSessionInfo ReconnectSessionInfo { get; private set; }
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
    private Coroutine _reconnectCoroutine;
    private Coroutine _reconnectTimeoutCoroutine;
    private bool _reconnectCompletionTriggered;
    private bool _reconnectFailureTriggered;
    private bool _suppressReconnectDisconnectFailure;

    public int GetBattlePos()
    {
        if (CurrentBattleType == BattleType.Remote &&
            ReconnectSessionInfo != null &&
            ReconnectSessionInfo.RoomId != 0 &&
            (IsReconnecting || IsBattleStart))
        {
            return ReconnectSessionInfo.PlayerPos;
        }

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
        ReconnectSessionInfo = new ReconnectSessionInfo();
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
        IsBattleConnected = false;
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

    public void BeginReconnectFromLogin(pb.S2C_LoginMsg loginMsg)
    {
        if (loginMsg == null || !loginMsg.CanReconnect)
            return;

        if (_reconnectCoroutine != null)
            StopCoroutine(_reconnectCoroutine);

        ReconnectSessionInfo.Reset();
        ReconnectSessionInfo.RoomId = loginMsg.ReconnectRoomId;
        ReconnectSessionInfo.PlayerId = PlayerId;
        ReconnectSessionInfo.AuthoritativeFrame = (int)loginMsg.ReconnectFrame;
        ReconnectSessionInfo.LastReceivedFrame = -1;
        ReconnectSessionInfo.PlayerPos = (int)loginMsg.ReconnectPlayerPos;
        foreach (var gamer in loginMsg.ReconnectGamers)
            ReconnectSessionInfo.Gamers.Add(gamer);

        if (ReconnectSessionInfo.Gamers.Count != GameSetting.RoomMaxPlayerCount ||
            ReconnectSessionInfo.PlayerPos < 0 ||
            ReconnectSessionInfo.PlayerPos >= GameSetting.RoomMaxPlayerCount ||
            !ReconnectSessionInfo.Gamers.Contains(PlayerId))
        {
            HandleBattleReconnectFailed("重连房间信息无效");
            return;
        }

        if (RoomInfo == null)
            RoomInfo = new RoomInfo();
        RoomInfo.RoomId = ReconnectSessionInfo.RoomId;
        RoomInfo.Readies.Clear();
        RoomInfo.Gamers.Clear();
        RoomInfo.Gamers.AddRange(ReconnectSessionInfo.Gamers);

        CurrentBattleType = BattleType.Remote;
        _reconnectCoroutine = StartCoroutine(BeginReconnectRoutine());
    }

    public uint GetReconnectRoomId()
    {
        return ReconnectSessionInfo == null ? 0 : ReconnectSessionInfo.RoomId;
    }

    public uint GetReconnectPlayerId()
    {
        return ReconnectSessionInfo == null || ReconnectSessionInfo.PlayerId == 0 ? PlayerId : ReconnectSessionInfo.PlayerId;
    }

    public int GetReconnectLastReceivedFrame()
    {
        return ReconnectSessionInfo == null ? 0 : ReconnectSessionInfo.LastReceivedFrame;
    }

    public void OnReconnectRequestSent()
    {
        UpdateReconnectStatus(
            ReconnectLoadingStatus.RequestingReconnect,
            BuildReconnectProgressText(GetReconnectLastReceivedFrame(), ReconnectSessionInfo == null ? 0 : ReconnectSessionInfo.AuthoritativeFrame),
            0f);
    }

    public void HandleBattleReconnectAccepted(int authoritativeFrame)
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectFailureTriggered)
            return;

        ReconnectSessionInfo.AuthoritativeFrame = authoritativeFrame;
        var progress = CalculateReconnectProgress01(ReconnectSessionInfo.LastReceivedFrame, authoritativeFrame);
        UpdateReconnectStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            BuildReconnectProgressText(ReconnectSessionInfo.LastReceivedFrame, authoritativeFrame),
            progress);

        if (authoritativeFrame <= ReconnectSessionInfo.LastReceivedFrame)
            CompleteReconnectCatchUp();
    }

    public void NotifyReconnectFrameSynced(int frame)
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectFailureTriggered)
            return;

        ReconnectSessionInfo.LastReceivedFrame = Math.Max(ReconnectSessionInfo.LastReceivedFrame, frame);
        var targetFrame = ReconnectSessionInfo.AuthoritativeFrame;
        var progress = CalculateReconnectProgress01(ReconnectSessionInfo.LastReceivedFrame, targetFrame);
        UpdateReconnectStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            BuildReconnectProgressText(ReconnectSessionInfo.LastReceivedFrame, targetFrame),
            progress);

        if (targetFrame <= ReconnectSessionInfo.LastReceivedFrame)
            CompleteReconnectCatchUp();
    }

    public void CompleteReconnectCatchUp()
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectCompletionTriggered || _reconnectFailureTriggered)
            return;

        _reconnectCompletionTriggered = true;
        StopReconnectTimeout();
        UpdateReconnectStatus(ReconnectLoadingStatus.EnteringBattle, "同步完成，正在进入战斗", 1f);

        var targetFrame = ReconnectSessionInfo.AuthoritativeFrame;
        if (!_battleController.FastForwardToFrame(targetFrame))
        {
            _reconnectCompletionTriggered = false;
            HandleBattleReconnectFailed("追帧失败");
            return;
        }

        CurrentBattleType = BattleType.Remote;
        IsReconnecting = false;
        IsBattleStart = true;
        IsBattleConnected = true;
        ServerAuthorityFrame = targetFrame;
        ReconnectSessionInfo.LastReceivedFrame = Math.Max(ReconnectSessionInfo.LastReceivedFrame, targetFrame);
        ReconnectSessionInfo.FailureReason = string.Empty;

        CameraManager.Instance.ApplyBattleCameraState();
        if (_battleView == null)
            CreateBattleView();

        _battleView.InitView(_battleController.DisplayBattleEntity);
        _battleViewInitialized = true;
        InitializeEntitySystems();
        UIRootManager.Instance.ShowRoot(UIRootType.Battle);
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
        BattleRecordManager.Instance.StartRecordBattle(GetBattlePos());

        _reconnectCompletionTriggered = false;
        _reconnectCoroutine = null;
    }

    public void HandleBattleReconnectFailed(string reason)
    {
        if (_reconnectFailureTriggered)
            return;

        _reconnectFailureTriggered = true;
        StopReconnectTimeout();
        if (ReconnectSessionInfo != null)
            ReconnectSessionInfo.FailureReason = string.IsNullOrEmpty(reason) ? "重连失败" : reason;

        UpdateReconnectStatus(
            ReconnectLoadingStatus.Failed,
            ReconnectSessionInfo == null ? "重连失败" : ReconnectSessionInfo.FailureReason,
            0f);

        if (_reconnectCoroutine != null)
            StopCoroutine(_reconnectCoroutine);
        _reconnectCoroutine = StartCoroutine(HandleBattleReconnectFailedRoutine(
            ReconnectSessionInfo == null ? "重连失败" : ReconnectSessionInfo.FailureReason));
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

    private IEnumerator BeginReconnectRoutine()
    {
        _reconnectCompletionTriggered = false;
        _reconnectFailureTriggered = false;
        IsReconnecting = true;
        IsBattleConnected = false;
        IsBattleStart = false;
        ServerAuthorityFrame = -1;
        UpdateReconnectStatus(ReconnectLoadingStatus.Preparing, "准备重连", 0f);
        StartReconnectTimeout();

        if (CurrentBattleType == BattleType.Replay)
        {
            StopReplayBattle();
        }
        else if (_battleView != null || IsBattleStart)
        {
            CleanupRemoteBattleForReconnect();
        }

        FrameBuffer.ResetForReconnect(ReconnectSessionInfo == null ? -1 : ReconnectSessionInfo.LastReceivedFrame);
        _battleController.ResetRuntimeState();

        UIRootManager.Instance.HideAllRoots();
        yield return SceneManager.LoadSceneAsync((int)EGameScene.ReconnectLoading, LoadSceneMode.Single);
        UIRootManager.Instance.ShowRoot(UIRootType.ReconnectLoading);

        UpdateReconnectStatus(ReconnectLoadingStatus.LoadingBattleScene, "加载战斗场景", 0f);
        yield return SceneManager.LoadSceneAsync((int)EGameScene.World, LoadSceneMode.Single);

        CreateBattleView();
        CameraManager.Instance.ApplyBattleCameraState();
        _battleController.InitEntities();

        UpdateReconnectStatus(ReconnectLoadingStatus.ConnectingServer, "连接服务器", 0f);
        NetworkManager.Instance.KcpConnect();
        NetworkManager.Instance.KcpUpdate();
        _reconnectCoroutine = null;
    }

    private IEnumerator HandleBattleReconnectFailedRoutine(string reason)
    {
        yield return new WaitForSeconds(1f);

        CleanupRemoteBattleForReconnect();
        IsReconnecting = false;
        IsBattleConnected = false;
        IsBattleStart = false;
        ServerAuthorityFrame = -1;

        yield return SceneManager.LoadSceneAsync((int)EGameScene.Main, LoadSceneMode.Single);
        CameraManager.Instance.ApplyMainCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Modern);
        OnStatusMessage?.Invoke(string.IsNullOrEmpty(reason) ? "重连失败" : reason);

        _reconnectFailureTriggered = false;
        _reconnectCompletionTriggered = false;
        _reconnectCoroutine = null;
    }

    private IEnumerator ReconnectTimeoutRoutine()
    {
        yield return new WaitForSeconds(ReconnectTimeoutSeconds);
        _reconnectTimeoutCoroutine = null;
        HandleBattleReconnectFailed("重连超时");
    }

    private void StartReconnectTimeout()
    {
        StopReconnectTimeout();
        _reconnectTimeoutCoroutine = StartCoroutine(ReconnectTimeoutRoutine());
    }

    private void StopReconnectTimeout()
    {
        if (_reconnectTimeoutCoroutine == null)
            return;

        StopCoroutine(_reconnectTimeoutCoroutine);
        _reconnectTimeoutCoroutine = null;
    }

    private void CleanupRemoteBattleForReconnect()
    {
        _suppressReconnectDisconnectFailure = true;
        try
        {
            StopRemoteBattle();
        }
        finally
        {
            _suppressReconnectDisconnectFailure = false;
        }
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

    private void UpdateReconnectStatus(ReconnectLoadingStatus status, string progressText)
    {
        UpdateReconnectStatus(status, progressText, ReconnectProgress01);
    }

    private void UpdateReconnectStatus(ReconnectLoadingStatus status, string progressText, float progress01)
    {
        ReconnectStatus = status;
        ReconnectProgressText = progressText ?? string.Empty;
        ReconnectProgress01 = Mathf.Clamp01(progress01);
        OnReconnectLoadingStatusChanged?.Invoke(ReconnectStatus, ReconnectProgressText);
    }

    private string BuildReconnectProgressText(int currentFrame, int targetFrame)
    {
        if (targetFrame <= 0)
            return "目标帧 0";

        return $"同步帧 {Math.Max(0, Math.Min(currentFrame, targetFrame))}/{targetFrame}";
    }

    private float CalculateReconnectProgress01(int currentFrame, int targetFrame)
    {
        if (targetFrame <= 0)
            return 1f;

        return Mathf.Clamp01(Math.Max(0, currentFrame) / (float)targetFrame);
    }

    public override void OnRelease()
    {
        StopReconnectTimeout();
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
