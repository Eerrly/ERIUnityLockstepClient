[PlayerState(EPlayerState.Move)]
public class PlayerMoveState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
        Logger.Log(LogLevel.Info, $"PlayerMoveState OnEnter PlayerId:{playerEntity.ID} MoveSpeed:{playerEntity.Movement.moveSpeed} TurnSpeed:{playerEntity.Movement.turnSpeed}");
    }

    public override void OnUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        MoveSystem.UpdatePosition(playerEntity);
        MoveSystem.UpdateRotation(playerEntity);
    }

    public override void OnLateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        Logger.Log(LogLevel.Info, $"PlayerMoveState OnLateUpdate " +
                                  $"PlayerId:{playerEntity.ID} " +
                                  $"Yaw:{playerEntity.Input.yaw}" +
                                  $"Key:{playerEntity.Input.key}" +
                                  $"Move.Position:{playerEntity.Movement.position} " +
                                  $"Move.Rotation:{playerEntity.Movement.rotation}");
    }

    public override void OnExit(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnExit(playerEntity, battleEntity);
        Logger.Log(LogLevel.Info, $"PlayerMoveState OnExit PlayerId:{playerEntity.ID}");
    }
}