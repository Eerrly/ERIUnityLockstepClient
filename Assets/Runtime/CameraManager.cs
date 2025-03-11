using System;
using UnityEngine;

public class CameraManager : MManager<CameraManager>
{
    [Header("Target")]
    [SerializeField] private PlayerView _player;
    
    [Header("UICamera")]
    [SerializeField] private Camera _uiCamera;
    
    [Header("BattleCamera")]
    [SerializeField] private Camera _battleCamera;

    public Vector3 BattleCameraPositionOffset = new Vector3(0, 2, -5);
    public float BattleCameraFollowSmoothness = 5f;

    private Vector3 _battleCameraVelocity;

    public override void Initialize()
    {
        _uiCamera = GameObject.Find("UICamera").GetComponent<Camera>();
        _battleCamera = GameObject.Find("BattleCamera").GetComponent<Camera>();
    }

    public override void OnRelease()
    {
        _player = null;
    }

    /// <summary>
    /// 开关UI相机
    /// </summary>
    public void ToggleUICamera()
    {
        _uiCamera.enabled = !_uiCamera.enabled;
    }

    /// <summary>
    /// 开关战斗相机
    /// </summary>
    public void ToggleBattleCamera()
    {
        _battleCamera.enabled = !_battleCamera.enabled;
    }

    /// <summary>
    /// 设置相机跟随的目标
    /// </summary>
    /// <param name="playerView"></param>
    public void InitPlayerView(PlayerView playerView)
    {
        _player = playerView;
        BattleCameraPositionOffset = _battleCamera.transform.position - _player.transform.position;
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

    private void LateUpdate()
    {
        HandleFollowBattleTarget();
    }
}