/// <summary>
/// 动画状态机对应参数
/// </summary>
public enum EBlendTreeParam
{
    /// <summary>
    /// 移动
    /// </summary>
    MoveForward,
    /// <summary>
    /// 转向
    /// </summary>
    Turn,
}

/// <summary>
/// 动画事件 (事件触发时间必须从小到大)
/// </summary>
public enum EAnimationEvent
{
    None = 0,
    /// <summary>
    /// 动画开始
    /// </summary>
    AnimStart = 1,
    /// <summary>
    /// 触发
    /// </summary>
    Fire = 2,
    /// <summary>
    /// 动画结束
    /// </summary>
    AnimEnd = 3,
    Count,
}

/// <summary>
/// 动画ID  (每一个动画都必须拥有上面所有的动画事件，循环动画除外)
/// </summary>
public enum EAnimationID
{
    None = -1,
    /// <summary>
    /// 移动/站立
    /// </summary>
    Locomotion = 0,
    /// <summary>
    /// 攻击
    /// </summary>
    Attack = 1,
    /// <summary>
    /// 收击
    /// </summary>
    Hit = 2,
}