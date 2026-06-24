using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class BattleSceneFlowController
{
    private readonly Func<EGameScene, IEnumerator> _loadScene;
    private readonly Action _applyMainCameraState;
    private readonly Action _applyBattleCameraState;
    private readonly Action _applyReplayCameraState;
    private readonly Action<UIRootType> _showRoot;
    private readonly Action _hideAllRoots;

    public BattleSceneFlowController()
        : this(
            LoadSceneRoutine,
            () => CameraManager.Instance.ApplyMainCameraState(),
            () => CameraManager.Instance.ApplyBattleCameraState(),
            () => CameraManager.Instance.ApplyReplayCameraState(),
            rootType => UIRootManager.Instance.ShowRoot(rootType),
            () => UIRootManager.Instance.HideAllRoots())
    {
    }

    public BattleSceneFlowController(
        Func<EGameScene, IEnumerator> loadScene,
        Action applyMainCameraState,
        Action applyBattleCameraState,
        Action applyReplayCameraState,
        Action<UIRootType> showRoot,
        Action hideAllRoots)
    {
        _loadScene = loadScene ?? throw new ArgumentNullException(nameof(loadScene));
        _applyMainCameraState = applyMainCameraState ?? throw new ArgumentNullException(nameof(applyMainCameraState));
        _applyBattleCameraState = applyBattleCameraState ?? throw new ArgumentNullException(nameof(applyBattleCameraState));
        _applyReplayCameraState = applyReplayCameraState ?? throw new ArgumentNullException(nameof(applyReplayCameraState));
        _showRoot = showRoot ?? throw new ArgumentNullException(nameof(showRoot));
        _hideAllRoots = hideAllRoots ?? throw new ArgumentNullException(nameof(hideAllRoots));
    }

    public IEnumerator LoadWorldScene(Action onLoaded)
    {
        return LoadSceneAndInvoke(EGameScene.World, onLoaded);
    }

    public IEnumerator LoadMainScene(Action onLoaded)
    {
        return LoadSceneAndInvoke(EGameScene.Main, onLoaded);
    }

    public IEnumerator LoadReconnectLoadingScene(Action onLoaded)
    {
        return LoadSceneAndInvoke(EGameScene.ReconnectLoading, onLoaded);
    }

    public void ShowMainPresentation()
    {
        _applyMainCameraState();
        _showRoot(UIRootType.Modern);
    }

    public void ApplyBattleCameraState()
    {
        _applyBattleCameraState();
    }

    public void ApplyReplayCameraState()
    {
        _applyReplayCameraState();
    }

    public void ShowBattleRoot()
    {
        _showRoot(UIRootType.Battle);
    }

    public void ShowReplayRoot()
    {
        _showRoot(UIRootType.Replay);
    }

    public void ShowReconnectLoadingRoot()
    {
        _showRoot(UIRootType.ReconnectLoading);
    }

    public void HideAllRoots()
    {
        _hideAllRoots();
    }

    private IEnumerator LoadSceneAndInvoke(EGameScene scene, Action onLoaded)
    {
        var routine = _loadScene(scene);
        while (routine.MoveNext())
        {
            yield return routine.Current;
        }

        onLoaded?.Invoke();
    }

    private static IEnumerator LoadSceneRoutine(EGameScene scene)
    {
        yield return SceneManager.LoadSceneAsync((int)scene, LoadSceneMode.Single);
    }
}
