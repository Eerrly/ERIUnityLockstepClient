/// <summary>
/// 玩家状态
/// </summary>
public enum EPlayerState
{
    None = 0,
    
    /// <summary>
    /// 移动
    /// </summary>
    Move = 1,
    
    /// <summary>
    /// 攻击
    /// </summary>
    Attack = 2,
    
    /// <summary>
    /// 收击
    /// </summary>
    Hit = 3,
    
    /// <summary>
    /// 死亡
    /// </summary>
    Dead = 4,
    
    Count,
}