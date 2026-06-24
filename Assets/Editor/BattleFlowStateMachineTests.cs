using NUnit.Framework;

public class BattleFlowStateMachineTests
{
    [Test]
    public void NewMachineStartsInMainAndKeepsRemoteBattleTypeCompatibility()
    {
        var stateMachine = new BattleFlowStateMachine();

        Assert.AreEqual(BattleRuntimeState.Main, stateMachine.CurrentState);
        Assert.AreEqual(BattleType.Remote, stateMachine.CurrentBattleType);
        Assert.IsFalse(stateMachine.IsReconnecting);
    }

    [Test]
    public void RemoteBattleCanEnterReconnectAndThenCompleteBackToRemote()
    {
        var stateMachine = new BattleFlowStateMachine();

        Assert.IsTrue(stateMachine.TryEnterBattle(BattleType.Remote, out _));
        Assert.IsTrue(stateMachine.TryEnterBattle(BattleType.Reconnect, out _));
        Assert.AreEqual(BattleRuntimeState.ReconnectBattle, stateMachine.CurrentState);
        Assert.IsTrue(stateMachine.IsReconnecting);

        Assert.IsTrue(stateMachine.TryEnter(BattleRuntimeState.RemoteBattle, out _));
        Assert.AreEqual(BattleRuntimeState.RemoteBattle, stateMachine.CurrentState);
    }

    [Test]
    public void ReplayBattleCannotSwitchDirectlyToRemoteBattle()
    {
        var stateMachine = new BattleFlowStateMachine();

        Assert.IsTrue(stateMachine.TryEnterBattle(BattleType.Replay, out _));
        Assert.IsFalse(stateMachine.TryEnterBattle(BattleType.Remote, out var reason));

        Assert.AreEqual(BattleRuntimeState.ReplayBattle, stateMachine.CurrentState);
        Assert.IsFalse(string.IsNullOrEmpty(reason));
    }

    [Test]
    public void ReconnectCanFailBackToMain()
    {
        var stateMachine = new BattleFlowStateMachine();

        Assert.IsTrue(stateMachine.TryEnterBattle(BattleType.Reconnect, out _));
        Assert.IsTrue(stateMachine.TryEnter(BattleRuntimeState.Main, out _));

        Assert.AreEqual(BattleRuntimeState.Main, stateMachine.CurrentState);
        Assert.IsFalse(stateMachine.IsReconnecting);
    }
}
