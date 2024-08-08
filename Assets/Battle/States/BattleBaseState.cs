public class BattleBaseState : BaseState<BattleEntity>
{
    public EBattleState StateId
    {
        get => (EBattleState)stateId;
        set => stateId = (int)value;
    }

    public override void Reset(BattleEntity entity, BattleEntity _)
    {
        entity.State.currStateId = (int)StateId;
        entity.State.nextStateId = 0;
    }

    public override void OnEnter(BattleEntity entity, BattleEntity _)
    {
        entity.State.enteTime = entity.Time;
    }

    public override void OnExit(BattleEntity entity, BattleEntity _)
    {
        entity.State.exitTime = entity.Time;
    }
    
}