[PlayerState(EPlayerState.Move)]
public class PlayerMoveState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
    }

    public override void OnUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        MoveSystem.UpdatePosition(playerEntity);
        MoveSystem.UpdateRotation(playerEntity);
    }

    public override void OnLateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        // Logger.Log(LogLevel.Info, $"PlayerMoveState OnLateUpdate " +
        //                           $"battleEntity:{battleEntity}" +
        //                           $"ID:{playerEntity.ID} " +
        //                           $"Y:{playerEntity.Input.yaw}" +
        //                           $"K:{playerEntity.Input.key}" +
        //                           $"P:{playerEntity.Movement.position} " +
        //                           $"R:{playerEntity.Movement.rotation}");
    }

    public override void OnExit(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnExit(playerEntity, battleEntity);
    }
}