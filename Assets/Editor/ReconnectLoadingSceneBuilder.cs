using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 生成断线重连加载界面资源，保证 Resources 路径、字段绑定和 Build Settings 一致。
/// </summary>
public static class ReconnectLoadingSceneBuilder
{
    private const string PrefabPath = "Assets/Resources/UI/ReconnectLoadingUIRoot.prefab";
    private const string ScenePath = "Assets/Scenes/ReconnectLoadingScene.unity";

    [MenuItem("Tools/Reconnect Loading/Build Scene And Prefab")]
    public static void BuildSceneAndPrefab()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/UI");
        EnsureFolder("Assets/Scenes");

        var prefab = CreatePrefab();
        CreateScene(prefab);
        EnsureSceneInBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("断线重连 Loading Scene 和 UI Prefab 已生成。");
    }

    private static GameObject CreatePrefab()
    {
        var root = CreateRectObject("ReconnectLoadingUIRoot", null);
        SetStretch(root.GetComponent<RectTransform>());
        var view = root.AddComponent<ReconnectLoadingRootView>();

        AddPanel(root.transform, "Background", StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, new Color(0.028f, 0.034f, 0.04f, 1f));
        AddPanel(root.transform, "MistVeil", StretchMin(), StretchMax(), Vector2.zero, Vector2.zero, new Color(0.02f, 0.075f, 0.078f, 0.34f));
        AddPanel(root.transform, "TopRule", new Vector2(0.08f, 1f), new Vector2(0.92f, 1f), new Vector2(0f, -72f), new Vector2(0f, 3f), new Color(0.56f, 0.42f, 0.22f, 0.85f));
        AddPanel(root.transform, "BottomRule", new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), new Vector2(0f, 72f), new Vector2(0f, 3f), new Color(0.56f, 0.42f, 0.22f, 0.65f));

        var panel = AddPanel(root.transform, "ReconnectPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 320f), new Color(0.055f, 0.065f, 0.072f, 0.93f));
        AddPanel(panel.transform, "PanelEdge", StretchMin(), StretchMax(), Vector2.zero, new Vector2(-18f, -18f), new Color(0.09f, 0.115f, 0.12f, 0.62f));
        AddPanel(panel.transform, "AccentLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(16f, 0f), new Vector2(4f, -34f), new Color(0.78f, 0.56f, 0.25f, 0.9f));
        AddPanel(panel.transform, "AccentRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-16f, 0f), new Vector2(4f, -34f), new Color(0.2f, 0.62f, 0.55f, 0.75f));

        var title = AddText(panel.transform, "TitleText", new Vector2(0.08f, 0.68f), new Vector2(0.92f, 0.88f), "断线重连", 44, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.94f, 0.9f, 0.78f, 1f));
        var status = AddText(panel.transform, "StatusText", new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.61f), "准备重连", 28, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.76f, 0.84f, 0.8f, 1f));
        var progress = AddText(panel.transform, "ProgressText", new Vector2(0.1f, 0.18f), new Vector2(0.9f, 0.34f), "等待战斗状态同步", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.58f, 0.66f, 0.64f, 1f));

        var barTrack = AddPanel(panel.transform, "ProgressTrack", new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.12f), Vector2.zero, new Vector2(0f, 8f), new Color(0.025f, 0.03f, 0.034f, 0.95f));
        AddPanel(barTrack.transform, "ProgressGlow", new Vector2(0f, 0f), new Vector2(0.42f, 1f), Vector2.zero, Vector2.zero, new Color(0.2f, 0.62f, 0.55f, 0.92f));
        AddPanel(panel.transform, "ScanLine", new Vector2(0.18f, 0.39f), new Vector2(0.82f, 0.39f), Vector2.zero, new Vector2(0f, 2f), new Color(0.78f, 0.56f, 0.25f, 0.45f));

        view.TitleText = title;
        view.StatusText = status;
        view.ProgressText = progress;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void CreateScene(GameObject prefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "ReconnectLoadingScene";

        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.028f, 0.034f, 0.04f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        var lightObject = new GameObject("Directional Light");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.7f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var canvasObject = new GameObject("ReconnectLoadingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = camera;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.transform.SetParent(canvasObject.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            SetStretch(instance.GetComponent<RectTransform>());
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        ConfigureRootCanvasRect(canvasObject.GetComponent<RectTransform>());
        Canvas.ForceUpdateCanvases();
        EditorUtility.SetDirty(canvasObject);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    private static void EnsureSceneInBuildSettings()
    {
        var existing = EditorBuildSettings.scenes;
        for (var i = 0; i < existing.Length; i++)
        {
            if (existing[i].path == ScenePath)
            {
                existing[i].enabled = true;
                EditorBuildSettings.scenes = existing;
                return;
            }
        }

        var next = new EditorBuildSettingsScene[existing.Length + 1];
        for (var i = 0; i < existing.Length; i++)
            next[i] = existing[i];
        next[next.Length - 1] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = next;
    }

    private static GameObject AddPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        var go = CreateRectObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static Text AddText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, FontStyle style, TextAnchor alignment, Color color)
    {
        var go = CreateRectObject(name, parent);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.raycastTarget = false;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(14, fontSize - 10);
        label.resizeTextMaxSize = fontSize;
        return label;
    }

    private static GameObject CreateRectObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetStretch(RectTransform rect)
    {
        rect.anchorMin = StretchMin();
        rect.anchorMax = StretchMax();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private static void ConfigureRootCanvasRect(RectTransform rect)
    {
        rect.anchorMin = StretchMin();
        rect.anchorMax = StretchMax();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.localPosition = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * 0.01f;
    }

    private static Vector2 StretchMin()
    {
        return new Vector2(0f, 0f);
    }

    private static Vector2 StretchMax()
    {
        return new Vector2(1f, 1f);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        var folder = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
