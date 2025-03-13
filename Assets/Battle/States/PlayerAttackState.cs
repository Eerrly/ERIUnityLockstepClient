/// <summary>
/// 玩家攻击状态
/// </summary>
[PlayerState(EPlayerState.Attack)]
public class PlayerAttackState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
        AnimationSystem.SetAnimation(playerEntity, battleEntity, EAnimationID.Attack);
    }

    public override void OnLateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        if (AnimationSystem.IsTriggerEvent(playerEntity, EAnimationEvent.AnimStart))
        {
            Logger.Log(LogLevel.Info, $"PlayerAttackState IsTriggerEvent AnimStart");
        }
        if (AnimationSystem.IsTriggerEvent(playerEntity, EAnimationEvent.Fire))
        {
            Logger.Log(LogLevel.Info, $"PlayerAttackState IsTriggerEvent Fire");
            for (int i = 0; i < battleEntity.PlayerEntities.Count; i++)
            {
                var other = battleEntity.PlayerEntities[i];
                if (other.ID == playerEntity.ID)
                    continue;
                var attackResult = AreaSystem.InAttackArea(playerEntity, other);
                if (attackResult)
                    other.State.nextStateId = (int)EPlayerState.Hit;
                Logger.Log(LogLevel.Info, $"PlayerAttackState InAttackArea source:{playerEntity.ID} other:{other.ID} attackResult:{attackResult}");
            }
        }
        if (AnimationSystem.IsTriggerEvent(playerEntity, EAnimationEvent.AnimEnd))
        {
            Logger.Log(LogLevel.Info, "PlayerAttackState IsTriggerEvent AnimEnd");
            playerEntity.State.nextStateId = (int)EPlayerState.Move;
        }
    }

    public override void OnExit(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnExit(playerEntity, battleEntity);
    }
}