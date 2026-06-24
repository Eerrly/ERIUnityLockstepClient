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
    public const string ReconnectCatchUpRoundCompleteReason = "CatchUpRoundComplete";
    public const string ReconnectCompleteReason = "ReconnectComplete";

    public event Action OnReplayFinished;
    public event Action<string> OnStatusMessage;
    public event Action<ReconnectLoadingStatus, string> OnReconnectLoadingStatusChanged;

    public uint PlayerId;
    public RoomInfo RoomInfo;
    public int ServerAuthorityFrame = -1;
    public bool IsBattleConnected = false;
    public bool IsBattleStart = false;
    public bool IsReconnecting => _battleStateMachine != null && _battleStateMachine.IsReconnecting;
    public bool ShouldIgnoreReconnectDisconnectFailure => _suppressReconnectDisconnectFailure;
    public ReconnectLoadingStatus ReconnectStatus { get; private set; } = ReconnectLoadingStatus.None;
    public string ReconnectProgressText { get; private set; } = string.Empty;
    public float ReconnectProgress01 { get; private set; }
    public ReconnectSessionInfo ReconnectSessionInfo { get; private set; }
    public BattleRuntimeState CurrentBattleState => _battleStateMachine == null ? BattleRuntimeState.Main : _battleStateMachine.CurrentState;
    public BattleType CurrentBattleType => _battleStateMachine == null ? BattleType.Remote : _battleStateMachine.CurrentBattleType;

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
    private BattleFlowStateMachine _battleStateMachine;
    private BattleSessionController _battleSessionController;
    private BattleReconnectController _battleReconnectController;
    private BattleViewLifecycleController _battleViewLifecycleController;
    private Coroutine _remoteExitCoroutine;
    private Coroutine _replayExitCoroutine;
    private Coroutine _reconnectCoroutine;
    private Coroutine _reconnectTimeoutCoroutine;
    private bool _reconnectCompletionTriggered;
    private bool _reconnectFailureTriggered;
    private bool _suppressReconnectDisconnectFailure;

    public int GetBattlePos()
    {
        if (ReconnectSessionInfo != null &&
            ReconnectSessionInfo.RoomId != 0 &&
            (CurrentBattleType == BattleType.Reconnect || (CurrentBattleType == BattleType.Remote && IsBattleStart)))
        {
            return ReconnectSessionInfo.PlayerPos;
        }

        return (int)(PlayerId - GameSetting.DefaultPlayerIdBase - 1);
    }

    public override void Initialize()
    {
        Application.targetFrameRate = GameSetting.TargetFrameRate;

        _battleStateMachine = new BattleFlowStateMachine();
        _battleSessionController = new BattleSessionController();
        _battleReconnectController = new BattleReconnectController();
        _battleViewLifecycleController = new BattleViewLifecycleController(_battleSessionController.BindBattleView);

        _battleController = _battleSessionController.BattleController;
        _replayController = _battleSessionController.ReplayController;
        _replayController.OnReplayFinished += HandleReplayFinished;
        _frameBuffer = _battleSessionController.FrameBuffer;
        _frameEngine = _battleSessionController.FrameEngine;
        ReconnectSessionInfo = _battleReconnectController.SessionInfo;
    }

    public void StartBattle(BattleType battleType)
    {
        var previousBattleType = CurrentBattleType;
        if (!_battleStateMachine.TryEnterBattle(battleType, out var failureReason))
        {
            Logger.Log(LogLevel.Warning, failureReason);
            return;
        }

        switch (battleType)
        {
            case BattleType.Remote:
                StartRemoteBattle();
                break;
            case BattleType.Replay:
                StartReplayBattle();
                break;
            case BattleType.Reconnect:
                StartReconnectBattle(previousBattleType);
                break;
        }
    }

    private void StartRemoteBattle()
    {
        IsBattleStart = true;
        CameraManager.Instance.ApplyBattleCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Battle);

        _battleController.InitEntities();
        _battleViewLifecycleController.InitView(_battleController.DisplayBattleEntity);
        InitializeEntitySystems();
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
        BattleRecordManager.Instance.StartRecordBattle(GetBattlePos());
    }

    private void StartReplayBattle()
    {
        IsBattleStart = true;
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            _battleViewLifecycleController.RecreateView();
            CameraManager.Instance.ApplyReplayCameraState();

            _replayController.InitReplay(GetBattlePos());
            _replayController.RestartReplay();
            UIRootManager.Instance.ShowRoot(UIRootType.Replay);
            _battleViewLifecycleController.InitView(_replayController.DisplayBattleEntity);
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
        if (!IsBattleStart || !_battleViewLifecycleController.HasView)
            return;

        try
        {
            switch (CurrentBattleType)
            {
                case BattleType.Remote:
                    _battleViewLifecycleController.RenderUpdate(_battleController.DisplayBattleEntity, deltaTime);
                    break;
                case BattleType.Replay:
                    _battleViewLifecycleController.RenderUpdate(_replayController.DisplayBattleEntity, deltaTime);
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
            case BattleType.Reconnect:
                StopReconnectBattle();
                break;
        }
        TransitionBattleState(BattleRuntimeState.Main);
    }

    public void StopBattle()
    {
        StopBattle(CurrentBattleType);
    }

    private void StopRemoteBattle()
    {
        if (!IsBattleStart && !_battleViewLifecycleController.HasView)
            return;

        _frameEngine?.StopEngine();
        _battleViewLifecycleController.ReleaseView(_battleController.DisplayBattleEntity);
        ReleaseEntitySystems();
        BattleRecordManager.Instance.OnRelease();
        NetworkManager.Instance.KcpShutdown();
        IsBattleConnected = false;
        IsBattleStart = false;
    }

    private void StopReplayBattle()
    {
        _frameEngine?.StopReplayEngine();
        _battleViewLifecycleController.ReleaseView(_replayController.DisplayBattleEntity);
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

        var serverReconnectFrame = (int)loginMsg.ReconnectFrame;
        _battleReconnectController.ResetFromLogin(loginMsg, PlayerId);
        ReconnectSessionInfo = _battleReconnectController.SessionInfo;
        Logger.Log(LogLevel.Info, $"[Reconnect] Begin from login room:{ReconnectSessionInfo.RoomId} player:{PlayerId} serverFrame:{serverReconnectFrame} localLastReceived:{ReconnectSessionInfo.LastReceivedFrame}");

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

        StartBattle(BattleType.Reconnect);
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

    public void HandleBattleReconnectAccepted(int authoritativeFrame, bool isCatchUpRoundComplete, bool isReconnectComplete)
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectFailureTriggered)
            return;

        StartReconnectTimeout();
        if (isCatchUpRoundComplete && !isReconnectComplete)
        {
            ReconnectSessionInfo.AuthoritativeFrame = authoritativeFrame;
        }
        else
        {
            ReconnectSessionInfo.AuthoritativeFrame = Math.Max(ReconnectSessionInfo.AuthoritativeFrame, authoritativeFrame);
        }
        ReconnectSessionInfo.IsCatchUpRoundComplete |= isCatchUpRoundComplete;
        ReconnectSessionInfo.IsReconnectComplete |= isReconnectComplete;
        var progress = CalculateReconnectProgress01(ReconnectSessionInfo.LastReceivedFrame, ReconnectSessionInfo.AuthoritativeFrame);
        UpdateReconnectStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            BuildReconnectProgressText(ReconnectSessionInfo.LastReceivedFrame, ReconnectSessionInfo.AuthoritativeFrame),
            progress);

        TryCompleteReconnectCatchUp();
    }

    public void NotifyReconnectFrameSynced(int frame)
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectFailureTriggered)
            return;

        StartReconnectTimeout();
        ReconnectSessionInfo.LastReceivedFrame = Math.Max(ReconnectSessionInfo.LastReceivedFrame, frame);
        ReconnectSessionInfo.AuthoritativeFrame = Math.Max(ReconnectSessionInfo.AuthoritativeFrame, frame);
        var targetFrame = ReconnectSessionInfo.AuthoritativeFrame;
        var progress = CalculateReconnectProgress01(ReconnectSessionInfo.LastReceivedFrame, targetFrame);
        UpdateReconnectStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            BuildReconnectProgressText(ReconnectSessionInfo.LastReceivedFrame, targetFrame),
            progress);

        TryCompleteReconnectCatchUp();
    }

    private void TryCompleteReconnectCatchUp()
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectCompletionTriggered || _reconnectFailureTriggered)
            return;

        if (!ReconnectSessionInfo.IsCatchUpRoundComplete && !ReconnectSessionInfo.IsReconnectComplete)
            return;

        if (ReconnectSessionInfo.LastReceivedFrame < ReconnectSessionInfo.AuthoritativeFrame)
            return;

        if (ReconnectSessionInfo.IsReconnectComplete)
        {
            CompleteReconnectCatchUp();
            return;
        }

        AcknowledgeReconnectCatchUp();
    }

    private void AcknowledgeReconnectCatchUp()
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectCompletionTriggered || _reconnectFailureTriggered)
            return;

        var targetFrame = ReconnectSessionInfo.AuthoritativeFrame;
        if (ReconnectSessionInfo.LastAckedFrame >= targetFrame)
            return;

        ReconnectSessionInfo.LastAckedFrame = targetFrame;
        ReconnectSessionInfo.IsCatchUpRoundComplete = false;
        UpdateReconnectStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            "等待服务器确认同步",
            CalculateReconnectProgress01(ReconnectSessionInfo.LastReceivedFrame, targetFrame));

        NetworkManager.Instance.SendBattleReconnectMessage(
            ReconnectSessionInfo.RoomId,
            ReconnectSessionInfo.PlayerId,
            targetFrame);
    }

    public void CompleteReconnectCatchUp()
    {
        if (!IsReconnecting || ReconnectSessionInfo == null || _reconnectCompletionTriggered || _reconnectFailureTriggered)
            return;

        _reconnectCompletionTriggered = true;
        StopReconnectTimeout();
        UpdateReconnectStatus(ReconnectLoadingStatus.EnteringBattle, "同步完成，正在进入战斗", 1f);

        var targetFrame = ReconnectSessionInfo.AuthoritativeFrame;
        try
        {
            FrameEngine.SetFrameInterval(BattleSetting.BattleInterval);
            if (!_battleController.FastForwardToFrame(targetFrame))
            {
                _reconnectCompletionTriggered = false;
                HandleBattleReconnectFailed("追帧失败");
                return;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"[Reconnect] FastForwardToFrame exception ->\n{ex.Message}\n{ex.StackTrace}");
            _reconnectCompletionTriggered = false;
            HandleBattleReconnectFailed("追帧失败");
            return;
        }

        TransitionBattleState(BattleRuntimeState.RemoteBattle);
        IsBattleStart = true;
        IsBattleConnected = true;
        ServerAuthorityFrame = targetFrame;
        _battleController.AlignServerTimeToFrame(targetFrame);
        ReconnectSessionInfo.LastReceivedFrame = Math.Max(ReconnectSessionInfo.LastReceivedFrame, targetFrame);
        ReconnectSessionInfo.FailureReason = string.Empty;

        CameraManager.Instance.ApplyBattleCameraState();
        _battleViewLifecycleController.InitView(_battleController.DisplayBattleEntity);
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
        TransitionBattleState(BattleRuntimeState.Main);
        _replayExitCoroutine = null;
    }

    private IEnumerator HandleRemoteBattleExitRoutine(string reason)
    {
        StopRemoteBattle();
        yield return SceneManager.LoadSceneAsync((int)EGameScene.Main, LoadSceneMode.Single);
        CameraManager.Instance.ApplyMainCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Modern);
        TransitionBattleState(BattleRuntimeState.Main);
        OnStatusMessage?.Invoke(string.IsNullOrEmpty(reason) ? "战斗已退出" : reason);
        _remoteExitCoroutine = null;
    }

    private void StartReconnectBattle(BattleType previousBattleType)
    {
        if (_reconnectCoroutine != null)
            StopCoroutine(_reconnectCoroutine);

        _reconnectCoroutine = StartCoroutine(BeginReconnectRoutine(previousBattleType));
    }

    private IEnumerator BeginReconnectRoutine(BattleType previousBattleType)
    {
        _reconnectCompletionTriggered = false;
        _reconnectFailureTriggered = false;
        UpdateReconnectStatus(ReconnectLoadingStatus.Preparing, "准备重连", 0f);
        StartReconnectTimeout();

        var wasBattleStart = IsBattleStart;
        if (previousBattleType == BattleType.Replay)
        {
            StopReplayBattle();
        }
        else if (_battleViewLifecycleController.HasView || wasBattleStart)
        {
            CleanupRemoteBattleForReconnect();
        }

        IsBattleConnected = false;
        IsBattleStart = false;
        ServerAuthorityFrame = -1;

        FrameBuffer.ResetForReconnect(-1);
        _battleController.ResetRuntimeState();

        UIRootManager.Instance.HideAllRoots();
        yield return SceneManager.LoadSceneAsync((int)EGameScene.ReconnectLoading, LoadSceneMode.Single);
        UIRootManager.Instance.ShowRoot(UIRootType.ReconnectLoading);

        UpdateReconnectStatus(ReconnectLoadingStatus.LoadingBattleScene, "加载战斗场景", 0f);
        yield return SceneManager.LoadSceneAsync((int)EGameScene.World, LoadSceneMode.Single);

        _battleViewLifecycleController.RecreateView();
        CameraManager.Instance.ApplyBattleCameraState();
        _battleController.InitEntities();
        InitializeEntitySystems();

        UpdateReconnectStatus(ReconnectLoadingStatus.ConnectingServer, "连接服务器", 0f);
        NetworkManager.Instance.KcpConnect();
        NetworkManager.Instance.KcpUpdate();
        _reconnectCoroutine = null;
    }

    private IEnumerator HandleBattleReconnectFailedRoutine(string reason)
    {
        yield return new WaitForSeconds(1f);

        CleanupRemoteBattleForReconnect();
        TransitionBattleState(BattleRuntimeState.Main);
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

    private void StopReconnectBattle()
    {
        StopReconnectTimeout();
        if (_reconnectCoroutine != null)
        {
            StopCoroutine(_reconnectCoroutine);
            _reconnectCoroutine = null;
        }

        CleanupRemoteBattleForReconnect();
        TransitionBattleState(BattleRuntimeState.Main);
        IsBattleConnected = false;
        IsBattleStart = false;
        ServerAuthorityFrame = -1;
        _reconnectCompletionTriggered = false;
        _reconnectFailureTriggered = false;
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

    public void OnRoomFull()
    {
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            _battleViewLifecycleController.RecreateView();
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
        _battleReconnectController?.UpdateStatus(ReconnectStatus, ReconnectProgressText, ReconnectProgress01);
        OnReconnectLoadingStatusChanged?.Invoke(ReconnectStatus, ReconnectProgressText);
    }

    private void TransitionBattleState(BattleRuntimeState nextState)
    {
        if (_battleStateMachine == null)
            return;

        if (!_battleStateMachine.TryEnter(nextState, out var failureReason))
            Logger.Log(LogLevel.Warning, failureReason);
    }

    private string BuildReconnectProgressText(int currentFrame, int targetFrame)
    {
        return _battleReconnectController == null
            ? string.Empty
            : _battleReconnectController.BuildProgressText(currentFrame, targetFrame);
    }

    private float CalculateReconnectProgress01(int currentFrame, int targetFrame)
    {
        return _battleReconnectController == null
            ? 0f
            : _battleReconnectController.CalculateProgress01(currentFrame, targetFrame);
    }

    public override void OnRelease()
    {
        StopReconnectTimeout();
        _battleSessionController?.Release();

        if (_replayController != null)
            _replayController.OnReplayFinished -= HandleReplayFinished;
    }
}
