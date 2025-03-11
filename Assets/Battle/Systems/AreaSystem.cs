using Framework;

/// <summary>
/// 区域系统
/// </summary>
[EntitySystem]
public class AreaSystem
{
    /// <summary>
    /// 场地区域（逻辑层用）
    /// </summary>
    public static readonly FixedRect Boundary = new FixedRect(
        FixedNumber.MakeFixNum(-6000, 1000),
        FixedNumber.MakeFixNum(6000, 1000),
        FixedNumber.MakeFixNum(12000, 1000),
        FixedNumber.MakeFixNum(12000, 1000)
    );

    /// <summary>
    /// 场地区域（显示层用）
    /// </summary>
    public static readonly UnityEngine.Rect BoundaryFloat = new UnityEngine.Rect(
        -6.0f,
        6.0f,
        12.0f,
        12.0f
    );

    /// <summary>
    /// 保证在区域内（逻辑层用）
    /// </summary>
    public static FixedVector3 MakeInside(FixedVector3 pos)
    {
        pos.x = FixedMath.Clamp(pos.x, Boundary.xMin, Boundary.xMax);
        pos.y = FixedMath.Max(FixedNumber.Zero, pos.y);
        pos.z = FixedMath.Clamp(pos.z, Boundary.yMin, Boundary.yMax);
        return pos;
    }

    /// <summary>
    /// 保证在区域内（显示层用）
    /// </summary>
    public static UnityEngine.Vector3 MakeInside(UnityEngine.Vector3 pos)
    {
        pos.x = UnityEngine.Mathf.Clamp(pos.x, BoundaryFloat.xMin, BoundaryFloat.xMax);
        pos.y = UnityEngine.Mathf.Max(0, pos.y);
        pos.z = UnityEngine.Mathf.Clamp(pos.z, BoundaryFloat.yMin, BoundaryFloat.yMax);
        return pos;
    }
    
}