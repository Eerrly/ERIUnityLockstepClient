using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 相机管理器
/// </summary>
public class CameraManager : MManager<CameraManager>
{
    [Header("Target")]
    [SerializeField] private PlayerView _player;
    
    [Header("UICamera")]
    [SerializeField] private Camera _uiCamera;
    
    [Header("BattleCamera")]
    [SerializeField] private Camera _battleCamera;

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

    private Vector3 _battleCameraVelocity;

    public override void Initialize()
    {
        var go = new GameObject("[EventSystem]");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        go.transform.SetParent(transform);

        BattleCameraRotationOffset = Quaternion.Euler(20, 0, 0);
        BattleCameraPositionOffset = new Vector3(0, 2, -5);
        BattleCameraFollowSmoothness = 5f;
    }

    public override void OnRelease()
    {
        _player = null;
    }

    /// <summary>
    /// 开关UI摄像机
    /// </summary>
    public void ToggleUICamera()
    {
        if (_uiCamera == null)
        {
            var go = new GameObject("[Camera - UI]");
            go.transform.SetParent(transform);
        
            _uiCamera = Util.GetOrAddComponent<Camera>(go);
            _uiCamera.clearFlags = CameraClearFlags.Depth;
            _uiCamera.cullingMask = 1 << 5;
            _uiCamera.orthographic = true;
            _uiCamera.transform.position = new Vector2(Screen.width / 2f, Screen.height / 2f);
            _uiCamera.nearClipPlane = -200000;
            _uiCamera.farClipPlane = 200000;
            _uiCamera.depth = 1;
            _uiCamera.allowHDR = false;
            _uiCamera.allowMSAA = false;
            _uiCamera.enabled = true;
        }
        else
            _uiCamera.enabled = !_uiCamera.enabled;
    }

    /// <summary>
    /// 给Canvas设置UI摄像机
    /// </summary>
    /// <param name="obj"></param>
    public void SetCanvasUICamera(GameObject obj)
    {
        var canvas = Util.GetOrAddComponent<Canvas>(obj);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = _uiCamera;
        canvas.planeDistance = 1;
    }

    /// <summary>
    /// 给Canvas设置战斗摄像机
    /// </summary>
    /// <param name="obj"></param>
    public void SetCanvasBattleCamera(GameObject obj)
    {
        var canvas = Util.GetOrAddComponent<Canvas>(obj);
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = _battleCamera;
        canvas.planeDistance = 10;
    }

    /// <summary>
    /// 开关战斗摄像机
    /// </summary>
    public void ToggleBattleCamera()
    {
        if (_battleCamera == null)
        {
            var go = new GameObject("[Camera - Battle]");
        
            _battleCamera = Util.GetOrAddComponent<Camera>(go);
            _battleCamera.transform.rotation = BattleCameraRotationOffset;
            
            _battleCamera.clearFlags = CameraClearFlags.Skybox;
            _battleCamera.fieldOfView = 60;
            _battleCamera.cullingMask = -1;
            _uiCamera.nearClipPlane = 0.3f;
            _uiCamera.farClipPlane = 1000;
            _battleCamera.orthographic = false;
            _battleCamera.depth = 0;
            _battleCamera.allowHDR = false;
            _battleCamera.allowMSAA = false;
            _battleCamera.enabled = true;
        }
        else
            _battleCamera.enabled = !_battleCamera.enabled;
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
        return _battleCamera.WorldToScreenPoint(pos);
    }

    private void LateUpdate()
    {
        HandleFollowBattleTarget();
    }
}