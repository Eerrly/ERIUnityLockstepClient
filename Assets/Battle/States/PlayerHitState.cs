/// <summary>
/// 收击状态
/// </summary>
[PlayerState(EPlayerState.Hit)]
public class PlayerHitState : PlayerBaseState
{
    public override void OnEnter(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnEnter(playerEntity, battleEntity);
        AnimationSystem.SetAnimation(playerEntity, battleEntity, EAnimationID.Hit);
    }

    public override void OnLateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        if (AnimationSystem.IsTriggerEvent(playerEntity, EAnimationEvent.AnimEnd))
            playerEntity.State.nextStateId = (int)EPlayerState.Move;
    }

    public override void OnExit(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        base.OnExit(playerEntity, battleEntity);
    }
}