/// <summary>
/// 输入系统
/// </summary>
[EntitySystem]
public class KeySystem
{
    /// <summary>
    /// 是否没有输入
    /// </summary>
    /// <param name="yaw">摇杆</param>
    /// <returns>是否没有输入</returns>
    public static bool IsYawTypeStop(int yaw)
    {
        return yaw == FixedMath.YawStop;
    }

    /// <summary>
    /// 检测是否按下J
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <returns>是否按下J</returns>
    public static bool CheckKeyCodeJDown(PlayerEntity entity)
    {
        var result = (entity.Input.key & 0x1) > 0;
        return result;
    }

    /// <summary>
    /// 检测是否按下K
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <returns>是否按下K</returns>
    public static bool CheckKeyCodeKDown(PlayerEntity entity)
    {
        var result = (entity.Input.key & 0x2) > 0;
        return result;
    }

    /// <summary>
    /// 检测是否按下L
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <returns>是否按下L</returns>
    public static bool CheckKeyCodeLDown(PlayerEntity entity)
    {
        var result = (entity.Input.key & 0x4) > 0;
        return result;
    }
}