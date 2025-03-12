using System;
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
    /// 玩家身上的动画状态机
    /// </summary>
    private Animator _animator;
    private static readonly int AnimatorMoveForwardHash = Animator.StringToHash("moveForward");
    private static readonly int AnimatorTurnHash = Animator.StringToHash("turn");
    private Vector3 _beforeRotationForward = Vector3.zero;
    private int _blendTreeParamLerpSpeed = 30;

    private float _animatorMoveForwardValue;
    private float _animatorTurnValue;
    
    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">玩家实体</param>
    public override void InitView(PlayerEntity entity)
    {
        ID = entity.ID;
        _instance = Instantiate(Resources.Load<GameObject>(BattleSetting.PlayerCharacterPath), Vector3.zero, Quaternion.identity);
        _animator = Util.GetOrAddComponent<Animator>(_instance);
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
        _beforeRotationForward = transform.forward;
        TransformUpdate(entity, deltaTime);
#if UNITY_EDITOR
        DebugTextContainer.Instance.SetText(transform, "State", entity.State.currStateId);
        DebugTextContainer.Instance.SetText(transform, "Yaw", entity.Input.yaw);
        DebugTextContainer.Instance.SetText(transform, "Key", entity.Input.key);
#endif
    }

    /// <summary>
    /// 渲染轮询
    /// </summary>
    /// <param name="entity"></param>
    public override void AfterRenderUpdate(PlayerEntity entity)
    {
        var angle = Vector3.SignedAngle(_beforeRotationForward, transform.forward, Vector3.up);
        angle = Mathf.Abs(angle) <= 1 ? 0 : angle;
        var turn = angle / entity.Movement.turnSpeed.ToFloat();

        SetMoveForward(KeySystem.IsYawTypeStop(entity.Input.yaw) ? 0f : 1.1f);
        SetTurn(turn);
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

    public void SetMoveForward(float value, bool updateImmediately = true)
    {
        if (Mathf.Abs(_animatorMoveForwardValue - value) > float.Epsilon)
        {
            if (_animatorMoveForwardValue < value)
                _animatorMoveForwardValue += Mathf.Min(value - _animatorMoveForwardValue, Time.deltaTime * _blendTreeParamLerpSpeed);
            else
                _animatorMoveForwardValue -= Mathf.Min(_animatorMoveForwardValue - value, Time.deltaTime * _blendTreeParamLerpSpeed);
        }
        if (_animator != null && updateImmediately)
        {
            _animator.SetFloat(AnimatorMoveForwardHash, _animatorMoveForwardValue);
        }
    }

    public void SetTurn(float value, bool updateImmediately = true)
    {
        if (Mathf.Abs(_animatorTurnValue - value) > float.Epsilon)
        {
            if (_animatorTurnValue < value)
                _animatorTurnValue += Mathf.Min(value - _animatorTurnValue, Time.deltaTime * _blendTreeParamLerpSpeed);
            else
                _animatorTurnValue -= Mathf.Min(_animatorTurnValue - value, Time.deltaTime * _blendTreeParamLerpSpeed);
        }
        if (_animator != null && updateImmediately)
        {
            _animator.SetFloat(AnimatorTurnHash, _animatorTurnValue);
        }
    }
    
}