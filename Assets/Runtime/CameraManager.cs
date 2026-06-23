using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 相机管理器
/// </summary>
public class CameraManager : MManager<CameraManager>
{
    private const int UiLayer = 5;

    [Header("Target")]
    [SerializeField] private PlayerView _player;

    [Header("UICamera")]
    [SerializeField] private Camera _uiCamera;

    [Header("BattleCamera")]
    [SerializeField] private Camera _battleCamera;

    [Header("EventSystem")]
    [SerializeField] private EventSystem _eventSystem;

    /// <summary>
    /// 战斗相机旋转偏移量
    /// </summary>
    public Quaternion BattleCameraRotationOffset;

    /// <summary>
    /// 战斗相机位置偏移量
    /// </summary>
    public Vector3 BattleCameraPositionOffset;

    /// <summary>
    /// 战斗相机平滑度
    /// </summary>
    public float BattleCameraFollowSmoothness;

    public override void Initialize()
    {
        EnsureEventSystem();

        BattleCameraRotationOffset = Quaternion.Euler(20, 0, 0);
        BattleCameraPositionOffset = new Vector3(0, 2, -5);
        BattleCameraFollowSmoothness = 5f;
    }

    public override void OnRelease()
    {
        _player = null;
        _eventSystem = null;
    }

    private void EnsureEventSystem()
    {
        if (_eventSystem != null)
            return;

        var go = new GameObject("[EventSystem]");
        _eventSystem = go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        go.transform.SetParent(transform);
    }

    /// <summary>
    /// 确保UI相机存在，不改变当前启用状态。
    /// </summary>
    public Camera EnsureUICamera()
    {
        if (_uiCamera != null)
            return _uiCamera;

        var go = new GameObject("[Camera - UI]");
        go.transform.SetParent(transform);

        _uiCamera = Util.GetOrAddComponent<Camera>(go);
        if (FindObjectOfType<AudioListener>() == null)
            Util.GetOrAddComponent<AudioListener>(go);

        _uiCamera.clearFlags = CameraClearFlags.Depth;
        _uiCamera.cullingMask = 1 << UiLayer;
        _uiCamera.orthographic = true;
        _uiCamera.transform.position = new Vector3(Screen.width / 2f, Screen.height / 2f, -10f);
        _uiCamera.nearClipPlane = -200000;
        _uiCamera.farClipPlane = 200000;
        _uiCamera.depth = 1;
        _uiCamera.allowHDR = false;
        _uiCamera.allowMSAA = false;
        _uiCamera.enabled = false;
        return _uiCamera;
    }

    /// <summary>
    /// 确保战斗相机存在，不改变当前启用状态。
    /// </summary>
    public Camera EnsureBattleCamera()
    {
        if (_battleCamera != null)
            return _battleCamera;

        var go = new GameObject("[Camera - Battle]");
        go.transform.SetParent(transform);

        _battleCamera = Util.GetOrAddComponent<Camera>(go);
        _battleCamera.transform.rotation = BattleCameraRotationOffset;
        _battleCamera.clearFlags = CameraClearFlags.Skybox;
        _battleCamera.fieldOfView = 60;
        _battleCamera.cullingMask = -1;
        _battleCamera.nearClipPlane = 0.3f;
        _battleCamera.farClipPlane = 1000;
        _battleCamera.orthographic = false;
        _battleCamera.depth = 0;
        _battleCamera.allowHDR = false;
        _battleCamera.allowMSAA = false;
        _battleCamera.enabled = false;
        return _battleCamera;
    }

    public void SetUICameraEnabled(bool enabled)
    {
        EnsureUICamera().enabled = enabled;
    }

    public void SetBattleCameraEnabled(bool enabled)
    {
        EnsureBattleCamera().enabled = enabled;
    }

    public void ApplyMainCameraState()
    {
        _player = null;
        ConfigureUICameraAsBase();
        SetUICameraEnabled(true);
        if (_battleCamera != null)
            _battleCamera.enabled = false;
    }

    public void ApplyBattleCameraState()
    {
        ConfigureBattleCameraStack();
        SetBattleCameraEnabled(true);
        SetUICameraEnabled(true);
    }

    public void ApplyReplayCameraState()
    {
        ConfigureBattleCameraStack();
        SetBattleCameraEnabled(true);
        SetUICameraEnabled(true);
    }

    /// <summary>
    /// 给Canvas设置UI摄像机
    /// </summary>
    /// <param name="obj"></param>
    public void SetCanvasUICamera(GameObject obj)
    {
        if (obj == null)
        {
            Logger.Log(LogLevel.Error, "SetCanvasUICamera target is null");
            return;
        }

        var canvas = Util.GetOrAddComponent<Canvas>(obj);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = EnsureUICamera();
        canvas.planeDistance = 1;
    }

    /// <summary>
    /// 给Canvas设置战斗摄像机
    /// </summary>
    /// <param name="obj"></param>
    public void SetCanvasBattleCamera(GameObject obj)
    {
        if (obj == null)
        {
            Logger.Log(LogLevel.Error, "SetCanvasBattleCamera target is null");
            return;
        }

        var canvas = Util.GetOrAddComponent<Canvas>(obj);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = EnsureBattleCamera();
        canvas.planeDistance = 10;
    }

    /// <summary>
    /// 设置相机跟随的目标
    /// </summary>
    /// <param name="playerView"></param>
    public void InitPlayerView(PlayerView playerView)
    {
        _player = playerView;
    }

    /// <summary>
    /// 跟随目标
    /// </summary>
    private void HandleFollowBattleTarget()
    {
        if (_battleCamera == null || _player == null)
            return;

        var targetCameraPosition = _player.transform.position + BattleCameraPositionOffset;
        _battleCamera.transform.position = Vector3.Lerp(
            _battleCamera.transform.position,
            targetCameraPosition,
            BattleCameraFollowSmoothness * Time.deltaTime);
    }

    /// <summary>
    /// 世界坐标转屏幕坐标
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Vector3 WorldToScreenPoint(Vector3 pos)
    {
        return EnsureBattleCamera().WorldToScreenPoint(pos);
    }

    private void ConfigureUICameraAsBase()
    {
        var uiCamera = EnsureUICamera();
        var uiCameraData = Util.GetOrAddComponent<UniversalAdditionalCameraData>(uiCamera.gameObject);
        uiCameraData.renderType = CameraRenderType.Base;

        if (_battleCamera == null)
            return;

        var battleCameraData = _battleCamera.GetComponent<UniversalAdditionalCameraData>();
        if (battleCameraData != null)
            RemoveCameraFromStack(battleCameraData, uiCamera);
    }

    private void ConfigureBattleCameraStack()
    {
        var battleCamera = EnsureBattleCamera();
        var uiCamera = EnsureUICamera();
        var battleCameraData = Util.GetOrAddComponent<UniversalAdditionalCameraData>(battleCamera.gameObject);
        var uiCameraData = Util.GetOrAddComponent<UniversalAdditionalCameraData>(uiCamera.gameObject);

        battleCameraData.renderType = CameraRenderType.Base;
        uiCameraData.renderType = CameraRenderType.Overlay;
        RemoveCameraFromStack(battleCameraData, uiCamera);
        battleCameraData.cameraStack.Add(uiCamera);
    }

    private static void RemoveCameraFromStack(UniversalAdditionalCameraData cameraData, Camera camera)
    {
        if (cameraData == null || camera == null)
            return;

        while (cameraData.cameraStack.Contains(camera))
        {
            cameraData.cameraStack.Remove(camera);
        }
    }

    private void LateUpdate()
    {
        HandleFollowBattleTarget();
    }
}
