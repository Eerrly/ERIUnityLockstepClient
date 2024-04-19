[EntitySystem]
public class KeySystem
{
    public static bool IsYawTypeStop(int yaw)
    {
        return yaw == FixedMath.YawStop;
    }

    public static bool CheckKeyCodeJDown(PlayerEntity entity)
    {
        var result = (entity.Input.key & 0x1) > 0;
        return result;
    }

    public static bool CheckKeyCodeKDown(PlayerEntity entity)
    {
        var result = (entity.Input.key & 0x2) > 0;
        return result;
    }

    public static bool CheckKeyCodeLDown(PlayerEntity entity)
    {
        var result = (entity.Input.key & 0x4) > 0;
        return result;
    }
}