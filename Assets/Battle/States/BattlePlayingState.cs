/// <summary>
/// 战斗进行中状态
/// </summary>
[BattleState(EBattleState.Playing)]
public class BattlePlayingState : BattleBaseState
{
    public override void OnEnter(BattleEntity entity, BattleEntity _)
    {
        base.OnEnter(entity, _);
        Logger.Log(LogLevel.Info, $"BattlePlayingState OnEnter " +
                                  $"Type:{System.Enum.GetName(typeof(EBattleEntityType), entity.BattleEntityType)} " +
                                  $"Frame:{entity.Frame} " +
                                  $"Time:{entity.Time}");
    }

    public override void OnUpdate(BattleEntity entity, BattleEntity _)
    {
        base.OnUpdate(entity, _);
    }

    public override void OnLateUpdate(BattleEntity entity, BattleEntity _)
    {
        base.OnLateUpdate(entity, _);
    }

    public override void OnExit(BattleEntity entity, BattleEntity _)
    {
        base.OnExit(entity, _);
        Logger.Log(LogLevel.Info, $"BattlePlayingState OnExit Frame:{entity.Frame} Time:{entity.Time} SpanTime:{entity.State.spanTime}");
    }
}