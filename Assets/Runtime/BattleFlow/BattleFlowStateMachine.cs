using System;

/// <summary>
/// 管理战斗流程状态和合法跳转，不负责场景、网络或实体释放。
/// </summary>
public class BattleFlowStateMachine
{
    public BattleRuntimeState CurrentState { get; private set; } = BattleRuntimeState.Main;
    public bool IsReconnecting => CurrentState == BattleRuntimeState.ReconnectBattle;
    public BattleType CurrentBattleType => ToBattleType(CurrentState);

    public bool TryEnterBattle(BattleType battleType, out string failureReason)
    {
        return TryEnter(ToRuntimeState(battleType), out failureReason);
    }

    public bool TryEnter(BattleRuntimeState nextState, out string failureReason)
    {
        if (!CanEnter(nextState))
        {
            failureReason = $"非法战斗状态跳转: {CurrentState} -> {nextState}";
            return false;
        }

        CurrentState = nextState;
        failureReason = string.Empty;
        return true;
    }

    public bool CanEnter(BattleRuntimeState nextState)
    {
        return CanTransition(CurrentState, nextState);
    }

    public static bool CanTransition(BattleRuntimeState currentState, BattleRuntimeState nextState)
    {
        if (currentState == nextState)
            return true;

        switch (currentState)
        {
            case BattleRuntimeState.Main:
                return nextState == BattleRuntimeState.RemoteBattle ||
                       nextState == BattleRuntimeState.ReplayBattle ||
                       nextState == BattleRuntimeState.ReconnectBattle;
            case BattleRuntimeState.RemoteBattle:
                return nextState == BattleRuntimeState.Main ||
                       nextState == BattleRuntimeState.ReconnectBattle;
            case BattleRuntimeState.ReplayBattle:
                return nextState == BattleRuntimeState.Main ||
                       nextState == BattleRuntimeState.ReconnectBattle;
            case BattleRuntimeState.ReconnectBattle:
                return nextState == BattleRuntimeState.Main ||
                       nextState == BattleRuntimeState.RemoteBattle;
            default:
                throw new ArgumentOutOfRangeException(nameof(currentState), currentState, null);
        }
    }

    public static BattleRuntimeState ToRuntimeState(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
                return BattleRuntimeState.RemoteBattle;
            case BattleType.Replay:
                return BattleRuntimeState.ReplayBattle;
            case BattleType.Reconnect:
                return BattleRuntimeState.ReconnectBattle;
            default:
                throw new ArgumentOutOfRangeException(nameof(battleType), battleType, null);
        }
    }

    public static BattleType ToBattleType(BattleRuntimeState runtimeState)
    {
        switch (runtimeState)
        {
            case BattleRuntimeState.Main:
            case BattleRuntimeState.RemoteBattle:
                return BattleType.Remote;
            case BattleRuntimeState.ReplayBattle:
                return BattleType.Replay;
            case BattleRuntimeState.ReconnectBattle:
                return BattleType.Reconnect;
            default:
                throw new ArgumentOutOfRangeException(nameof(runtimeState), runtimeState, null);
        }
    }
}
