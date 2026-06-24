using System;
using System.Collections;
using UnityEngine;

public class BattleReturnToMainFlowController
{
    private readonly Func<Action, IEnumerator> _loadMainScene;
    private readonly Action _showMainPresentation;
    private readonly Action _transitionToMain;
    private readonly Action<string> _publishStatus;
    private readonly Func<float, IEnumerator> _waitSeconds;

    public BattleReturnToMainFlowController(
        BattleSceneFlowController sceneFlowController,
        Action transitionToMain,
        Action<string> publishStatus)
    {
        if (sceneFlowController == null)
            throw new ArgumentNullException(nameof(sceneFlowController));

        _loadMainScene = sceneFlowController.LoadMainScene;
        _showMainPresentation = sceneFlowController.ShowMainPresentation;
        _transitionToMain = transitionToMain ?? throw new ArgumentNullException(nameof(transitionToMain));
        _publishStatus = publishStatus ?? throw new ArgumentNullException(nameof(publishStatus));
        _waitSeconds = WaitSecondsRoutine;
    }

    public BattleReturnToMainFlowController(
        Func<Action, IEnumerator> loadMainScene,
        Action showMainPresentation,
        Action transitionToMain,
        Action<string> publishStatus,
        Func<float, IEnumerator> waitSeconds)
    {
        _loadMainScene = loadMainScene ?? throw new ArgumentNullException(nameof(loadMainScene));
        _showMainPresentation = showMainPresentation ?? throw new ArgumentNullException(nameof(showMainPresentation));
        _transitionToMain = transitionToMain ?? throw new ArgumentNullException(nameof(transitionToMain));
        _publishStatus = publishStatus ?? throw new ArgumentNullException(nameof(publishStatus));
        _waitSeconds = waitSeconds ?? throw new ArgumentNullException(nameof(waitSeconds));
    }

    public IEnumerator ExitReplayToMain(Action stopReplayBattle, Action onComplete)
    {
        if (stopReplayBattle == null)
            throw new ArgumentNullException(nameof(stopReplayBattle));

        stopReplayBattle();
        yield return LoadMainWithPresentationAndState();
        onComplete?.Invoke();
    }

    public IEnumerator ExitRemoteBattleToMain(Action stopRemoteBattle, string reason, Action onComplete)
    {
        if (stopRemoteBattle == null)
            throw new ArgumentNullException(nameof(stopRemoteBattle));

        stopRemoteBattle();
        yield return LoadMainWithPresentationAndState();
        _publishStatus(string.IsNullOrEmpty(reason) ? "战斗已退出" : reason);
        onComplete?.Invoke();
    }

    public IEnumerator ReturnToMainAfterReconnectFailure(
        Action cleanupBattle,
        Action resetReconnectRuntimeState,
        string reason,
        Action onComplete)
    {
        if (cleanupBattle == null)
            throw new ArgumentNullException(nameof(cleanupBattle));
        if (resetReconnectRuntimeState == null)
            throw new ArgumentNullException(nameof(resetReconnectRuntimeState));

        yield return _waitSeconds(1f);

        cleanupBattle();
        _transitionToMain();
        resetReconnectRuntimeState();

        yield return _loadMainScene(_showMainPresentation);
        _publishStatus(string.IsNullOrEmpty(reason) ? "重连失败" : reason);
        onComplete?.Invoke();
    }

    private IEnumerator LoadMainWithPresentationAndState()
    {
        yield return _loadMainScene(() =>
        {
            _showMainPresentation();
            _transitionToMain();
        });
    }

    private static IEnumerator WaitSecondsRoutine(float seconds)
    {
        yield return new WaitForSeconds(seconds);
    }
}
