using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MManager<AnimationManager>
{
    private Dictionary<EBlendTreeParam, int> _blendTreeParamHashCacheDic;
    private Dictionary<EAnimationID, int> _animationHashCacheDic;
    private Dictionary<int, Animator> _playerAnimatorDic;

    [Header("Attack Animation Length (MS)")]
    public int attackAnimEndLength = 16000;
    public int attackFireLength = 8000;
    [Header("Hit Animation Length (ms)")] 
    public int hitAnimEndLength = 12000;
    public int hitFireLength = 200;
    
    public override void Initialize()
    {
        _blendTreeParamHashCacheDic = new Dictionary<EBlendTreeParam, int>();
        _animationHashCacheDic = new Dictionary<EAnimationID, int>();
        _playerAnimatorDic = new Dictionary<int, Animator>();
    }

    public override void OnRelease()
    {
        _playerAnimatorDic.Clear();
    }

    /// <summary>
    /// 设置玩家对应的状态机组件
    /// </summary>
    /// <param name="id"></param>
    /// <param name="animator"></param>
    public void SetPlayerAnimator(int id, Animator animator)
    {
        _playerAnimatorDic[id] = animator;
    }

    /// <summary>
    /// 给状态机动画传递参数
    /// </summary>
    public void SetFloatValue(int id, EBlendTreeParam param, float value)
    {
        if (!_blendTreeParamHashCacheDic.TryGetValue(param, out var blendTreeParamHash))
        {
            blendTreeParamHash = Animator.StringToHash(Enum.GetName(typeof(EBlendTreeParam), param));
            _blendTreeParamHashCacheDic[param] = blendTreeParamHash;
        }
        _playerAnimatorDic[id].SetFloat(blendTreeParamHash, value);
    }

    /// <summary>
    /// 给状态机动画传递参数
    /// </summary>
    public void SetIntValue(int id, EBlendTreeParam param, int value)
    {
        if (!_blendTreeParamHashCacheDic.TryGetValue(param, out var blendTreeParamHash))
        {
            blendTreeParamHash = Animator.StringToHash(Enum.GetName(typeof(EBlendTreeParam), param));
            _blendTreeParamHashCacheDic[param] = blendTreeParamHash;
        }
        _playerAnimatorDic[id].SetInteger(blendTreeParamHash, value);
    }

    /// <summary>
    /// 获取动画对应事件长度
    /// </summary>
    public FixedNumber GetAnimationLength(EAnimationID animationId, EAnimationEvent animationEvent)
    {
        // 开始帧为0
        if (animationEvent == EAnimationEvent.AnimStart)
            return FixedNumber.Zero;
        if (animationId == EAnimationID.Attack)
        {
            switch (animationEvent)
            {
                case EAnimationEvent.Fire:
                    return FixedNumber.MakeFixNum(attackFireLength, 10000);
                    break;
                case EAnimationEvent.AnimEnd:
                    return FixedNumber.MakeFixNum(attackAnimEndLength, 10000);
                    break;
            }
        }
        else if (animationId == EAnimationID.Hit)
        {
            switch (animationEvent)
            {
                case EAnimationEvent.Fire:
                    return FixedNumber.MakeFixNum(hitFireLength, 10000);
                    break;
                case EAnimationEvent.AnimEnd:
                    return FixedNumber.MakeFixNum(hitAnimEndLength, 10000);
                    break;
            }
        }
        return default;
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    public void CrossFadeInFixedTime(int id, EAnimationID animationId, float transitionDuration)
    {
        if (!_animationHashCacheDic.TryGetValue(animationId, out var animationHash))
        {
            animationHash = Animator.StringToHash(Enum.GetName(typeof(EAnimationID), animationId));
            _animationHashCacheDic[animationId] = animationHash;
        }
        _playerAnimatorDic[id].CrossFadeInFixedTime(animationHash, transitionDuration, 0);
        _playerAnimatorDic[id].Update(0);
    }
    
}