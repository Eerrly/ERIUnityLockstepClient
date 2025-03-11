using UnityEngine;

/// <summary>
/// 玩家渲染类
/// </summary>
public class PlayerView : BaseView<PlayerEntity>
{
    /// <summary>
    /// 玩家ID
    /// </summary>
    public int ID;
    /// <summary>
    /// 修正向量
    /// </summary>
    private Vector3 _fixV;
    /// <summary>
    /// 生成的玩家Prefab GameObject
    /// </summary>
    private GameObject _instance;
    
    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">玩家实体</param>
    public override void InitView(PlayerEntity entity)
    {
        ID = entity.ID;
        _instance = Instantiate(Resources.Load<GameObject>(BattleSetting.PlayerCharacterPath), Vector3.zero, Quaternion.identity);
        _instance.transform.SetParent(transform, false);
        var meshRenders = _instance.GetComponentsInChildren<MeshRenderer>();
        foreach (var render in meshRenders)
            render.material.color = BattleSetting.InitPlayerColor[entity.ID];
    }

    /// <summary>
    /// 渲染轮询
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="deltaTime"></param>
    public override void RenderUpdate(PlayerEntity entity, float deltaTime)
    {
        TransformUpdate(entity, deltaTime);
#if UNITY_EDITOR
        DebugTextContainer.Instance.SetText(transform, "State", entity.State.currStateId);
        DebugTextContainer.Instance.SetText(transform, "Yaw", entity.Input.yaw);
        DebugTextContainer.Instance.SetText(transform, "Key", entity.Input.key);
#endif
    }

    /// <summary>
    /// 更新位移以及旋转
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <param name="deltaTime">增量时间</param>
    private void TransformUpdate(PlayerEntity entity, float deltaTime)
    {
        var currentPosition = transform.position;
        var entityPosition = entity.Transform.pos.ToVector3();
        currentPosition += entity.Movement.position.ToVector3();
        currentPosition = Vector3.Lerp(currentPosition, entityPosition, 0);
        
        var currentRotation = transform.rotation;
        var nextDeltaRotation = entity.Movement.rotation.ToQuaternion();
        if(currentRotation != nextDeltaRotation)
        {
            var forward = MoveSystem.GetForwardAngle(entity).ToFloat();
            var target = MoveSystem.GetTargetAngle(entity).ToFloat();
            var angle = Mathf.MoveTowardsAngle(forward, target, entity.Movement.turnSpeed.ToFloat() * deltaTime);
            currentRotation = Quaternion.Euler(0f, angle, 0f);
        }

        var offset = entityPosition - currentPosition;
        var dis = 0.3f;
        if (offset.magnitude > dis)
        {
            var target = currentPosition + offset.normalized * (offset.magnitude - dis);
            currentPosition = Vector3.SmoothDamp(currentPosition, target, ref _fixV, 0.2f);
        }
        else
        {
            _fixV = Vector3.zero;
        }

        var transform1 = transform;
        transform1.position = AreaSystem.MakeInside(currentPosition);
        transform1.rotation = currentRotation;
    }
}