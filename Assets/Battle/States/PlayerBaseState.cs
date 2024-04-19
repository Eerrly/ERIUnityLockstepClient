[PlayerState(EPlayerState.None)]
public class PlayerBaseState : BaseState<PlayerEntity>
{
    public EPlayerState StateId
    {
        get => (EPlayerState)stateId;
        set => stateId = (int)value;
    }

    public override void Reset(PlayerEntity entity, BattleEntity battleEntity)
    {
        entity.State.currStateId = (int)StateId;
        entity.State.nextStateId = 0;
    }

    public override void OnEnter(PlayerEntity entity, BattleEntity battleEntity)
    {
        entity.State.enteTime = battleEntity.Time;
    }

    public override void OnExit(PlayerEntity entity, BattleEntity battleEntity)
    {
        entity.State.exitTime = battleEntity.Time;
    }
}