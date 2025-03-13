/// <summary>
/// 位移系统
/// </summary>
[EntitySystem]
public class MoveSystem
{
    /// <summary>
    /// 更新玩家位移信息
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <param name="dt">增量时间</param>
    public static void UpdatePosition(PlayerEntity entity, FixedNumber dt)
    {
        if (!KeySystem.IsYawTypeStop(entity.Input.yaw))
            entity.Movement.direction = FixedMath.FromYawToVector3(entity.Input.yaw).YZero();
        else
            entity.Movement.direction = FixedVector3.Zero;
        entity.Movement.position += entity.Movement.direction * entity.Movement.moveSpeed * dt;
    }

    /// <summary>
    /// 更新玩家旋转信息
    /// </summary>
    /// <param name="entity">玩家实体</param>
    public static void UpdateRotation(PlayerEntity entity)
    {
        if(!KeySystem.IsYawTypeStop(entity.Input.yaw))
            entity.Movement.rotation = FixedMath.FromYaw(entity.Input.yaw);
        entity.Movement.curYAngle = FixedMath.Yaw(entity.Movement.rotation * FixedVector3.Forward);
    }

    /// <summary>
    /// 通过位移信息和旋转信息来更新玩家的位置信息
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <param name="delta">增量时间</param>
    public static void TransformLogicUpdate(PlayerEntity entity, FixedNumber delta)
    {
        var currentPosition = entity.Transform.pos + entity.Movement.position;
        var currentRotation = entity.Transform.rot;
        var nextDeltaRotation = entity.Movement.rotation;
        if(currentRotation != nextDeltaRotation)
        {
            var angle = FixedMath.MoveTowardsAngle(GetForwardAngle(entity), GetTargetAngle(entity), entity.Movement.turnSpeed * delta);
            currentRotation = FixedQuaternion.Euler(FixedNumber.Zero, angle, FixedNumber.Zero);
        }

        entity.Transform.pos = AreaSystem.MakeInside(currentPosition);
        entity.Transform.rot = currentRotation;

        entity.Movement.position = FixedVector3.Zero;
        entity.Transform.forward = currentRotation * FixedVector3.Forward;
    }

    /// <summary>
    /// 获取当前角度
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <returns>当前角度</returns>
    public static FixedNumber GetForwardAngle(PlayerEntity entity)
    {
        return FixedMath.Yaw(entity.Transform.rot * FixedVector3.Forward);
    }

    /// <summary>
    /// 获取目标角度
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <returns>目标角度</returns>
    public static FixedNumber GetTargetAngle(PlayerEntity entity)
    {
        return entity.Movement.curYAngle > 180 ? -(360 - entity.Movement.curYAngle) : entity.Movement.curYAngle;
    }
    
}