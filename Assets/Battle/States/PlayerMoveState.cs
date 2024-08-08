[PlayerState(EPlayerState.Move)]
public class PlayerMoveState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
    }

    public override void OnUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        MoveSystem.UpdatePosition(playerEntity, FrameEngine.FrameInterval);
        MoveSystem.UpdateRotation(playerEntity);
    }

    public override void OnLateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        
    }

    public override void OnCollision(PlayerEntity source, PlayerEntity target, BattleEntity battleEntity)
    {
        Logger.Log(LogLevel.Info, $"PlayerMoveState OnCollision SourceId:{source.ID} TargetId:{target.ID} Frame:{battleEntity.Frame}");
    }

    public override void OnExit(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnExit(playerEntity, battleEntity);
    }
}