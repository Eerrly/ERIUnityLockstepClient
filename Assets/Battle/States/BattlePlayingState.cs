[BattleState(EBattleState.Playing)]
public class BattlePlayingState : BattleBaseState
{
    public override void OnEnter(BattleEntity entity, BattleEntity _)
    {
        Logger.Log(LogLevel.Info, $"BattlePlayingState OnEnter Name:{entity.Name} Frame:{entity.Frame} Time:{entity.Time}");
    }

    public override void OnUpdate(BattleEntity entity, BattleEntity battleEntity)
    {
    }

    public override void OnLateUpdate(BattleEntity entity, BattleEntity battleEntity)
    {
    }

    public override void OnExit(BattleEntity entity, BattleEntity _)
    {
        Logger.Log(LogLevel.Info, $"BattlePlayingState OnExit Frame:{entity.Frame} Time:{entity.Time}");
    }
}