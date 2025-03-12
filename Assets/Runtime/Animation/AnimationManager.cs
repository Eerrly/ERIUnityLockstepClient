using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画状态机对应参数
/// </summary>
public enum EBlendTreeParam
{
    MoveForward,
    Turn,
}

public class AnimationManager : MManager<AnimationManager>
{
    private Dictionary<EBlendTreeParam, int> _blendTreeParamHashCacheDic;
    private Dictionary<int, Animator> _playerAnimatorDic;
    
    public override void Initialize()
    {
        _blendTreeParamHashCacheDic = new Dictionary<EBlendTreeParam, int>();
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
            blendTreeParamHash = Animator.StringToHash(Enum.GetName(typeof(EBlendTreeParam), param));
        _playerAnimatorDic[id].SetFloat(blendTreeParamHash, value);
    }

    /// <summary>
    /// 给状态机动画传递参数
    /// </summary>
    public void SetIntValue(int id, EBlendTreeParam param, int value)
    {
        if (!_blendTreeParamHashCacheDic.TryGetValue(param, out var blendTreeParamHash))
            blendTreeParamHash = Animator.StringToHash(Enum.GetName(typeof(EBlendTreeParam), param));
        _playerAnimatorDic[id].SetInteger(blendTreeParamHash, value);
    }

    /// <summary>
    /// 播放动画
    /// </summary>
    public void Play(int id, string stateName)
    {
        _playerAnimatorDic[id].Play(stateName);
    }

    private void LateUpdate()
    {
        
    }
    
}