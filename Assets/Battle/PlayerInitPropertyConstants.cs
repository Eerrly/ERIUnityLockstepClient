/// <summary>
/// 初始化属性常量
/// </summary>
public class PlayerInitPropertyConstants
{
    /// <summary>
    /// 移动速度
    /// </summary>
    public static readonly FixedNumber MoveSpeed = FixedNumber.MakeFixNum(5, 1);
    
    /// <summary>
    /// 转向速度
    /// </summary>
    public static readonly FixedNumber TurnSpeed = FixedNumber.MakeFixNum(180, 1);
    
    /// <summary>
    /// 碰撞体积
    /// </summary>
    public static readonly FixedNumber CollisionSize = FixedNumber.MakeFixNum(4, 10);
    
    /// <summary>
    /// 攻击距离
    /// </summary>
    public static readonly FixedNumber AttackDistance = FixedNumber.One;
    
    /// <summary>
    /// 攻击扇形夹角
    /// </summary>
    public static readonly FixedNumber AttackAngle = FixedNumber.MakeFixNum(45, 1);
    
    /// <summary>
    /// 攻击掉血
    /// </summary>
    public static readonly System.Int32 AttackDamage = 1;

    /// <summary>
    /// 初始化总血量
    /// </summary>
    public static readonly System.Int32 TotalHp = 2;
}