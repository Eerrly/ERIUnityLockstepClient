/// <summary>
/// 玩家死亡状态
/// </summary>
[PlayerState(EPlayerState.Dead)]
public class PlayerDeadState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
        playerEntity.Property.effect = (int)EEffectType.HitBlood;
        AnimationSystem.SetAnimation(playerEntity, battleEntity, EAnimationID.Dead);
    }
    
}