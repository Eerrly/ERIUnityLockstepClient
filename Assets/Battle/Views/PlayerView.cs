using System;
using System.Collections;
using System.Collections.Generic;
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
    /// 生成的玩家Prefab GameObject
    /// </summary>
    private GameObject _instance;
    /// <summary>
    /// 玩家身上的动画状态机
    /// </summary>
    private Animator _animator;
    /// <summary>
    /// 玩家移动前的朝向
    /// </summary>
    private Vector3 _beforeRotationForward = Vector3.zero;
    /// <summary>
    /// 动画混合树参数插值速度
    /// </summary>
    private const int BlendTreeParamLerpSpeed = 30;
    /// <summary>
    /// 动画混合树参数 移动
    /// </summary>
    private float _animatorMoveForwardValue;
    /// <summary>
    /// 动画混合树参数 转向
    /// </summary>
    private float _animatorTurnValue;
    /// <summary>
    /// 上一次的动画ID
    /// </summary>
    private EAnimationID _lastAnimationId = EAnimationID.None;
    /// <summary>
    /// 当前的特效ID
    /// </summary>
    private int _currentEffect;
    /// <summary>
    /// 特效缓存字典
    /// </summary>
    private Dictionary<int, GameObject> _effectCacheDic = new Dictionary<int, GameObject>();
    /// <summary>
    /// 最近一帧的偏差
    /// </summary>
    private float _lastFrameSpeed;
    /// <summary>
    /// SmoothDamp 的平滑时间
    /// </summary>
    private float _fixTime = 0.1f;
    /// <summary>
    /// SmoothDamp 的速度缓存
    /// </summary>
    private Vector3 _fixV;

    
    /// <summary>
    /// 初始化渲染
    /// </summary>
    /// <param name="entity">玩家实体</param>
    public override void InitView(PlayerEntity entity)
    {
        ID = entity.ID;
        _instance = Instantiate(Resources.Load<GameObject>(PlayerSetting.PlayerCharacterPath + ID), Vector3.zero, Quaternion.identity);
        _instance.transform.SetParent(transform, false);

        _animator = Util.GetOrAddComponent<Animator>(_instance);
        AnimationManager.Instance.SetPlayerAnimator(entity.ID, _animator);
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
        EffectUpdate(entity);
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
    /// 特效轮询
    /// </summary>
    /// <param name="entity"></param>
    private void EffectUpdate(PlayerEntity entity)
    {
        if (_currentEffect == entity.Property.effect)
            return;
        if (entity.Property.effect == 0)
        {
            if (_currentEffect != (int)EEffectType.None)
            {
                _effectCacheDic[_currentEffect].SetActive(false);
                _currentEffect = (int)EEffectType.None;
            }
            return;
        }

        _currentEffect = entity.Property.effect;
        var effectTypeName = Enum.GetName(typeof(EEffectType), entity.Property.effect);
        StartCoroutine(LoadEffectAsync(entity, "Data/Prefabs/" + effectTypeName));
    }

    /// <summary>
    /// 异步加载特效资源
    /// </summary>
    private IEnumerator LoadEffectAsync(PlayerEntity entity, string effectPath)
    {
        if (_effectCacheDic.TryGetValue(entity.Property.effect, out var effectObj))
        {
            effectObj.SetActive(true);
            yield break;
        }
        var request = Resources.LoadAsync<GameObject>(effectPath);
        while (request.isDone == false)
            yield return null;
        effectObj = Instantiate(request.asset, Vector3.zero, Quaternion.identity) as GameObject;
        if (effectObj == null)
            yield break;
        effectObj.transform.SetParent(transform, false);
        effectObj.transform.position += Vector3.up;
        effectObj.transform.localScale *= 2;
        _effectCacheDic[_currentEffect] = effectObj;
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

        // 移动速度叠加 (提前应用逻辑层的 Movement 位移)
        currentPosition += entity.Movement.position.ToVector3();

        // 平滑角度旋转
        var currentRotation = transform.rotation;
        var targetRotation = entity.Movement.rotation.ToQuaternion();
        if (currentRotation != targetRotation)
        {
            var forward = transform.eulerAngles.y;
            var target = MoveSystem.GetTargetAngle(entity).ToFloat();
            var turnSpeed = entity.Movement.turnSpeed.ToFloat();

            var angle = Mathf.MoveTowardsAngle(forward, target, turnSpeed * deltaTime);
            currentRotation = Quaternion.Euler(0f, angle, 0f);
        }

        // 位置误差判断处理
        var offset = entityPosition - currentPosition;
        var distance = offset.magnitude;

        switch (distance)
        {
            case < 0.05f:
                // 靠近时直接吸附
                currentPosition = entityPosition;
                _fixV = Vector3.zero;
                _fixTime = 0.05f;
                break;
            case < 0.5f:
                // 中等距离：视觉平滑
                currentPosition = Vector3.Lerp(currentPosition, entityPosition, deltaTime * 10f);
                break;
            default:
                // 远距离：需要追赶，使用 SmoothDamp 动态调整
                var acceleration = (distance - _lastFrameSpeed) / deltaTime;
                // 根据追赶加速度调整平滑时间（越远追得越快）
                _fixTime = Mathf.Clamp(0.05f + acceleration * 0.01f, 0.05f, 0.2f);
                // 保持一个距离缓冲（0.3f），避免直接吸附造成跳动
                var target = currentPosition + offset.normalized * (distance - 0.3f);
                currentPosition = Vector3.SmoothDamp(currentPosition, target, ref _fixV, _fixTime);
                break;
        }

        _lastFrameSpeed = distance;

        // 应用位置和旋转
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