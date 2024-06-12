using UnityEngine;

[EntitySystem]
public class MoveSystem
{
    public static void UpdatePosition(PlayerEntity entity)
    {
        var position = FixedVector3.Zero;
        if(!KeySystem.IsYawTypeStop(entity.Input.yaw))
        {
            position = FixedMath.FromYawToVector3(entity.Input.yaw).YZero();
        }
        entity.Movement.position = position;
    }

    public static void UpdateRotation(PlayerEntity entity)
    {
        var rotation = FixedQuaternion.Identity;
        if(!KeySystem.IsYawTypeStop(entity.Input.yaw))
        {
            rotation = FixedMath.FromYaw(entity.Input.yaw);
        }
        entity.Movement.rotation = rotation;
        entity.Movement.curYAngle = FixedMath.Yaw(entity.Movement.rotation * FixedVector3.Forward);
    }

    public static void TransformLogicUpdate(PlayerEntity entity, FixedNumber delta)
    {
        var currentPosition = entity.Transform.pos;
        var moveDelta = entity.Movement.position * entity.Movement.moveSpeed;
        var nextDeltaPosition = currentPosition + moveDelta;
        if((currentPosition - nextDeltaPosition).sqrMagnitudeLongXZ >= 4)
        {
            currentPosition = FixedVector3.Lerp(currentPosition, nextDeltaPosition, delta);
        }
        var currentRotation = entity.Transform.rot;
        var nextDeltaRotation = entity.Movement.rotation;
        if(currentRotation != nextDeltaRotation)
        {
            var angle = FixedMath.MoveTowardsAngle(GetForwardAngle(entity), GetTargetAngle(entity), entity.Movement.turnSpeed * delta);
            currentRotation = FixedQuaternion.Euler(FixedNumber.Zero, angle, FixedNumber.Zero);
        }

        entity.Transform.pos = currentPosition;
        entity.Transform.rot = currentRotation;
    }

    public static FixedNumber GetForwardAngle(PlayerEntity entity)
    {
        return FixedMath.Yaw(entity.Transform.rot * FixedVector3.Forward);
    }

    public static FixedNumber GetTargetAngle(PlayerEntity entity)
    {
        return entity.Movement.curYAngle > 180 ? -(360 - entity.Movement.curYAngle) : entity.Movement.curYAngle;
    }
    
}