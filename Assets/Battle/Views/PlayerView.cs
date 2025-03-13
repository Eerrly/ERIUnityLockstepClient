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
    private Vector3 _beforeRotationForward = Vector3.zero;
    private const int BlendTreeParamLerpSpeed = 30;

    private float _animatorMoveForwardValue;
    private float _animatorTurnValue;

    private EAnimationID _lastAnimationId = EAnimationID.None;
    
    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">玩家实体</param>
    public override void InitView(PlayerEntity entity)
    {
        ID = entity.ID;
        _instance = Instantiate(Resources.Load<GameObject>(BattleSetting.PlayerCharacterPath + ID), Vector3.zero, Quaternion.identity);
        _instance.transform.SetParent(transform, false);

        _animator = Util.GetOrAddComponent<Animator>(_instance);
        AnimationManager.Instance.SetPlayerAnimator(entity.ID, _animator);
        
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
        AnimationUpdate(entity);
#if UNITY_EDITOR
        DebugTextContainer.Instance.SetText(transform, "State", entity.State.currStateId);
        DebugTextContainer.Instance.SetText(transform, "Yaw", entity.Input.yaw);
        DebugTextContainer.Instance.SetText(transform, "Key", entity.Input.key);
        DebugTextContainer.Instance.SetText(transform, "Anim", (int)entity.Animation.animId);
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

        SetMoveForward(entity.ID, KeySystem.IsYawTypeStop(entity.Input.yaw) ? 0f : 1.1f);
        SetTurn(entity.ID, turn);
    }

    /// <summary>
    /// 动画轮询
    /// </summary>
    private void AnimationUpdate(PlayerEntity entity)
    {
        if (_lastAnimationId != entity.Animation.animId)
        {
            _lastAnimationId = entity.Animation.animId;
            AnimationManager.Instance.CrossFadeInFixedTime(entity.ID, entity.Animation.animId, AnimationSystem.DefaultTransitionDuration.ToFloat());
        }
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

    /// <summary>
    /// 设置移动动画
    /// </summary>
    private void SetMoveForward(int id, float value, bool updateImmediately = true)
    {
        if (Mathf.Abs(_animatorMoveForwardValue - value) > float.Epsilon)
        {
            if (_animatorMoveForwardValue < value)
                _animatorMoveForwardValue += Mathf.Min(value - _animatorMoveForwardValue, Time.deltaTime * BlendTreeParamLerpSpeed);
            else
                _animatorMoveForwardValue -= Mathf.Min(_animatorMoveForwardValue - value, Time.deltaTime * BlendTreeParamLerpSpeed);
        }
        if (_animator != null && updateImmediately)
        {
            AnimationManager.Instance.SetFloatValue(id, EBlendTreeParam.MoveForward, _animatorMoveForwardValue);
        }
    }

    /// <summary>
    /// 设置转向动画
    /// </summary>
    private void SetTurn(int id, float value, bool updateImmediately = true)
    {
        if (Mathf.Abs(_animatorTurnValue - value) > float.Epsilon)
        {
            if (_animatorTurnValue < value)
                _animatorTurnValue += Mathf.Min(value - _animatorTurnValue, Time.deltaTime * BlendTreeParamLerpSpeed);
            else
                _animatorTurnValue -= Mathf.Min(_animatorTurnValue - value, Time.deltaTime * BlendTreeParamLerpSpeed);
        }
        if (_animator != null && updateImmediately)
        {
            AnimationManager.Instance.SetFloatValue(id, EBlendTreeParam.Turn, _animatorTurnValue);
        }
    }
    
}