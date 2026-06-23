using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI根节点管理器
/// </summary>
public class UIRootManager : MManager<UIRootManager>
{
    private const string ModernRootPath = "UI/ModernUIRoot";
    private const string BattleRootPath = "UI/BattleUIRoot";
    private const string ReplayRootPath = "UI/ReplayUIRoot";
    private const string ReconnectLoadingRootPath = "UI/ReconnectLoadingUIRoot";
    private const string UILayerName = "UI";

    private readonly Dictionary<UIRootType, GameObject> _roots = new Dictionary<UIRootType, GameObject>();
    private Canvas _rootCanvas;
    private UIRootType _activeRootType = UIRootType.None;

    public UIRootType ActiveRootType => _activeRootType;

    public override void Initialize()
    {
        EnsureRootCanvas();
        EnsureRoot(UIRootType.Modern);
        EnsureRoot(UIRootType.Battle);
        EnsureRoot(UIRootType.Replay);
        EnsureRoot(UIRootType.ReconnectLoading);
        HideAllRoots();
    }

    public GameObject ShowRoot(UIRootType rootType)
    {
        EnsureRootCanvas();
        HideAllRoots();

        var root = EnsureRoot(rootType);
        if (root == null)
        {
            _activeRootType = UIRootType.None;
            return null;
        }

        root.SetActive(true);
        _activeRootType = rootType;
        return root;
    }

    public T GetRootView<T>(UIRootType rootType) where T : Component
    {
        var root = EnsureRoot(rootType);
        return root == null ? null : root.GetComponent<T>();
    }

    public void HideAllRoots()
    {
        foreach (var root in _roots.Values)
        {
            if (root != null)
                root.SetActive(false);
        }

        _activeRootType = UIRootType.None;
    }

    public override void OnRelease()
    {
        foreach (var root in _roots.Values)
        {
            if (root != null)
                Destroy(root);
        }

        _roots.Clear();

        if (_rootCanvas != null)
        {
            Destroy(_rootCanvas.gameObject);
            _rootCanvas = null;
        }
    }

    private void EnsureRootCanvas()
    {
        var uiLayer = LayerMask.NameToLayer(UILayerName);
        if (_rootCanvas != null)
        {
            SetLayerRecursively(_rootCanvas.gameObject, uiLayer);
            return;
        }

        var canvasGo = new GameObject("UIRootCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        SetLayerRecursively(canvasGo, uiLayer);
        _rootCanvas = canvasGo.GetComponent<Canvas>();
        _rootCanvas.sortingOrder = 1000;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        CameraManager.Instance.SetCanvasUICamera(canvasGo);
        DontDestroyOnLoad(canvasGo);
    }

    private GameObject EnsureRoot(UIRootType rootType)
    {
        if (rootType == UIRootType.None)
            return null;

        if (_roots.TryGetValue(rootType, out var existingRoot))
            return existingRoot;

        var path = GetRootPath(rootType);
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null)
        {
            Logger.Log(LogLevel.Error, $"UIRoot prefab not found: Resources/{path}");
            return null;
        }

        var root = Instantiate(prefab, _rootCanvas.transform, false);
        root.name = $"{rootType}UIRoot";
        SetLayerRecursively(root, LayerMask.NameToLayer(UILayerName));
        EnsureRootView(rootType, root);
        root.SetActive(false);
        _roots[rootType] = root;
        return root;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private string GetRootPath(UIRootType rootType)
    {
        switch (rootType)
        {
            case UIRootType.Modern:
                return ModernRootPath;
            case UIRootType.Battle:
                return BattleRootPath;
            case UIRootType.Replay:
                return ReplayRootPath;
            case UIRootType.ReconnectLoading:
                return ReconnectLoadingRootPath;
            default:
                return string.Empty;
        }
    }

    private void EnsureRootView(UIRootType rootType, GameObject root)
    {
        switch (rootType)
        {
            case UIRootType.Modern:
                Util.GetOrAddComponent<ModernUIRootView>(root);
                break;
            case UIRootType.Battle:
                Util.GetOrAddComponent<BattleUIRootView>(root);
                break;
            case UIRootType.Replay:
                Util.GetOrAddComponent<ReplayUIRootView>(root);
                break;
            case UIRootType.ReconnectLoading:
                Util.GetOrAddComponent<ReconnectLoadingRootView>(root);
                break;
        }
    }
}
