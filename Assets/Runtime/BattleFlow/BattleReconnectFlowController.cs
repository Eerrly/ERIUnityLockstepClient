using System;
using System.Collections;

public class BattleReconnectFlowController
{
    private readonly Action _hideAllRoots;
    private readonly Func<Action, IEnumerator> _loadReconnectLoadingScene;
    private readonly Func<Action, IEnumerator> _loadWorldScene;
    private readonly Action _showReconnectLoadingRoot;
    private readonly Action _recreateBattleView;
    private readonly Action _applyBattleCameraState;
    private readonly Action _initBattleEntities;
    private readonly Action _initializeEntitySystems;
    private readonly Action _startBattleConnection;
    private readonly Action<ReconnectLoadingStatus, string, float> _updateReconnectStatus;

    public BattleReconnectFlowController(
        BattleSceneFlowController sceneFlowController,
        BattleViewLifecycleController viewLifecycleController,
        BattleEntitySystemLifecycleController entitySystemLifecycleController,
        BattleNetworkLifecycleController networkLifecycleController,
        Action<ReconnectLoadingStatus, string, float> updateReconnectStatus,
        Action initBattleEntities)
    {
        if (sceneFlowController == null)
            throw new ArgumentNullException(nameof(sceneFlowController));
        if (viewLifecycleController == null)
            throw new ArgumentNullException(nameof(viewLifecycleController));
        if (entitySystemLifecycleController == null)
            throw new ArgumentNullException(nameof(entitySystemLifecycleController));
        if (networkLifecycleController == null)
            throw new ArgumentNullException(nameof(networkLifecycleController));

        _hideAllRoots = sceneFlowController.HideAllRoots;
        _loadReconnectLoadingScene = sceneFlowController.LoadReconnectLoadingScene;
        _loadWorldScene = sceneFlowController.LoadWorldScene;
        _showReconnectLoadingRoot = sceneFlowController.ShowReconnectLoadingRoot;
        _recreateBattleView = () => viewLifecycleController.RecreateView();
        _applyBattleCameraState = sceneFlowController.ApplyBattleCameraState;
        _initBattleEntities = initBattleEntities ?? throw new ArgumentNullException(nameof(initBattleEntities));
        _initializeEntitySystems = entitySystemLifecycleController.Initialize;
        _startBattleConnection = networkLifecycleController.StartBattleConnection;
        _updateReconnectStatus = updateReconnectStatus ?? throw new ArgumentNullException(nameof(updateReconnectStatus));
    }

    public BattleReconnectFlowController(
        Action hideAllRoots,
        Func<Action, IEnumerator> loadReconnectLoadingScene,
        Func<Action, IEnumerator> loadWorldScene,
        Action showReconnectLoadingRoot,
        Action recreateBattleView,
        Action applyBattleCameraState,
        Action initBattleEntities,
        Action initializeEntitySystems,
        Action startBattleConnection,
        Action<ReconnectLoadingStatus, string, float> updateReconnectStatus)
    {
        _hideAllRoots = hideAllRoots ?? throw new ArgumentNullException(nameof(hideAllRoots));
        _loadReconnectLoadingScene = loadReconnectLoadingScene ?? throw new ArgumentNullException(nameof(loadReconnectLoadingScene));
        _loadWorldScene = loadWorldScene ?? throw new ArgumentNullException(nameof(loadWorldScene));
        _showReconnectLoadingRoot = showReconnectLoadingRoot ?? throw new ArgumentNullException(nameof(showReconnectLoadingRoot));
        _recreateBattleView = recreateBattleView ?? throw new ArgumentNullException(nameof(recreateBattleView));
        _applyBattleCameraState = applyBattleCameraState ?? throw new ArgumentNullException(nameof(applyBattleCameraState));
        _initBattleEntities = initBattleEntities ?? throw new ArgumentNullException(nameof(initBattleEntities));
        _initializeEntitySystems = initializeEntitySystems ?? throw new ArgumentNullException(nameof(initializeEntitySystems));
        _startBattleConnection = startBattleConnection ?? throw new ArgumentNullException(nameof(startBattleConnection));
        _updateReconnectStatus = updateReconnectStatus ?? throw new ArgumentNullException(nameof(updateReconnectStatus));
    }

    public IEnumerator EnterReconnectBattleScene()
    {
        _hideAllRoots();
        yield return _loadReconnectLoadingScene(_showReconnectLoadingRoot);

        _updateReconnectStatus(ReconnectLoadingStatus.LoadingBattleScene, "加载战斗场景", 0f);
        yield return _loadWorldScene(null);

        _recreateBattleView();
        _applyBattleCameraState();
        _initBattleEntities();
        _initializeEntitySystems();

        _updateReconnectStatus(ReconnectLoadingStatus.ConnectingServer, "连接服务器", 0f);
        _startBattleConnection();
    }
}
