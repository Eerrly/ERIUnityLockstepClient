/// <summary>
/// 玩家移动状态
/// </summary>
[PlayerState(EPlayerState.Move)]
public class PlayerMoveState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
        AnimationSystem.SetAnimation(playerEntity, battleEntity, EAnimationID.Locomotion, true);
    }

    public override void OnUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnUpdate(playerEntity, battleEntity);
        MoveSystem.UpdatePosition(playerEntity, FrameEngine.FrameInterval);
        MoveSystem.UpdateRotation(playerEntity);
    }

    public override void OnLateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        if (KeySystem.CheckKeyCodeJDown(playerEntity))
        {
            if (battleEntity.Time >= playerEntity.Property.attackCdTime)
            {
                playerEntity.Property.attackCdTime = battleEntity.Time + PlayerSetting.AttackCd;
                playerEntity.State.nextStateId = (int)EPlayerState.Attack;
            }
            else
                Logger.Log(LogLevel.Info, $"Attack CD attackCdTime:{playerEntity.Property.attackCdTime} time:{battleEntity.Time}");
        }
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