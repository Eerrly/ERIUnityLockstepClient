public class PlayerSetting
{
    /// <summary>
    /// 玩家实体预制体
    /// </summary>
    public static readonly string PlayerCharacterPath = "Data/Prefabs/Player";
    
    /// <summary>
    /// 战斗实体预制体
    /// </summary>
    public static readonly string BattleViewPath = "Data/Prefabs/Plane";

    /// <summary>
    /// 攻击CD
    /// </summary>
    public static readonly FixedNumber AttackCd = FixedNumber.MakeFixNum(30000, 10000);
}