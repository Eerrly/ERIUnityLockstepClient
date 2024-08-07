using UnityEngine;

[EntitySystem]
public class MoveSystem
{
    public void Initialize()
    {
        
    }
    
    public static void UpdatePosition(PlayerEntity entity, FixedNumber dt)
    {
        if (!KeySystem.IsYawTypeStop(entity.Input.yaw))
            entity.Movement.direction = FixedMath.FromYawToVector3(entity.Input.yaw).YZero();
        else
            entity.Movement.direction = FixedVector3.Zero;
        entity.Movement.position += entity.Movement.direction * entity.Movement.moveSpeed * dt;
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
        var currentPosition = entity.Transform.pos + entity.Movement.position;
        var currentRotation = entity.Transform.rot;
        var nextDeltaRotation = entity.Movement.rotation;
        if(currentRotation != nextDeltaRotation)
        {
            var angle = FixedMath.MoveTowardsAngle(GetForwardAngle(entity), GetTargetAngle(entity), entity.Movement.turnSpeed * delta);
            currentRotation = FixedQuaternion.Euler(FixedNumber.Zero, angle, FixedNumber.Zero);
        }

        entity.Transform.pos = currentPosition;
        entity.Transform.rot = currentRotation;

        entity.Movement.position = FixedVector3.Zero;
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