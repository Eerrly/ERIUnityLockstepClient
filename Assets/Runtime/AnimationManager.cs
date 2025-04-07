using System;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MManager<AnimationManager>
{
    private Dictionary<EBlendTreeParam, int> _blendTreeParamHashCacheDic;
    private Dictionary<EAnimationID, int> _animationHashCacheDic;
    private Dictionary<int, Animator> _playerAnimatorDic;

    private readonly Dictionary<EAnimationID, AnimationData> _playerAnimationDataDic = new();
    private readonly Dictionary<EAnimationID, string> _playerAnimationPathDic = new()
    {
        [EAnimationID.Attack] = "Data/AnimationData/zombie_light_attack_02",
        [EAnimationID.Hit] = "Data/AnimationData/zombie_hit_react_F_01"
    };

    private Dictionary<EAnimationID, Dictionary<EAnimationEvent, FixedNumber>> _playerAnimationEventLengthCacheDic;
    
    public override void Initialize()
    {
        _blendTreeParamHashCacheDic = new Dictionary<EBlendTreeParam, int>();
        _animationHashCacheDic = new Dictionary<EAnimationID, int>();
        _playerAnimatorDic = new Dictionary<int, Animator>();
        _playerAnimationEventLengthCacheDic = new Dictionary<EAnimationID, Dictionary<EAnimationEvent, FixedNumber>>();
        
        foreach (var playerAnimPathKv in _playerAnimationPathDic)
        {
            if (!_playerAnimationEventLengthCacheDic.TryGetValue(playerAnimPathKv.Key, out var animEventLengthDic))
            {
                animEventLengthDic = new Dictionary<EAnimationEvent, FixedNumber>();
                _playerAnimationEventLengthCacheDic[playerAnimPathKv.Key] = animEventLengthDic;
            }

            var animationData = Resources.Load<AnimationData>(playerAnimPathKv.Value);
            _playerAnimationDataDic.Add(playerAnimPathKv.Key, animationData);
            foreach (var animEvent in animationData.eventList)
            {
                animEventLengthDic[(EAnimationEvent)animEvent.type] = FixedNumber.MakeFixNum(3333333 * animEvent.frame, 100000000);
            }
            animEventLengthDic[EAnimationEvent.AnimEnd] = FixedNumber.MakeFixNum((long)(animationData.length * 10000), 10000);
        }

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
        Logger.Log(LogLevel.Info, $"GetAnimationLength " +
                                  $"EAnimationID: {System.Enum.GetName(typeof(EAnimationID), animationId)} " +
                                  $"EAnimationEvent: {System.Enum.GetName(typeof(EAnimationEvent), animationEvent)} ");

        if (_playerAnimationEventLengthCacheDic.TryGetValue(animationId, out var animEventLengthDic))
        {
            if (animEventLengthDic.TryGetValue(animationEvent, out var length))
                return length;
            else
                Logger.Log(LogLevel.Error, $"GetAnimationLength EAnimationEvent: {animationEvent} Not Found!");
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

    /// <summary>
    /// 检测动画是否不是循环动画
    /// </summary>
    public bool CheckAnimationNotLoop(EAnimationID animationId)
    {
        return _playerAnimationEventLengthCacheDic.TryGetValue(animationId, out var animEventLengthDic)
               && animEventLengthDic.Count > 0;
    }

    /// <summary>
    /// 获取动画信息
    /// </summary>
    public AnimationData GetAnimationData(EAnimationID animationId)
    {
        if (_playerAnimationDataDic.TryGetValue(animationId, out var animationData))
            return animationData;
        return null;
    }
    
}