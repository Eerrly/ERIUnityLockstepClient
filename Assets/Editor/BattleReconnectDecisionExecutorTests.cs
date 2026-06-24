using System.Collections.Generic;
using NUnit.Framework;

public class BattleReconnectDecisionExecutorTests
{
    [Test]
    public void SendAckDecisionSendsReconnectAckWhenAllowed()
    {
        var calls = new List<string>();
        var sessionInfo = new ReconnectSessionInfo
        {
            RoomId = 10001,
            PlayerId = 101,
        };
        var executor = CreateExecutor(calls, true, sessionInfo);

        executor.Handle(new ReconnectCatchUpDecision(ReconnectCatchUpAction.SendAck, 18));

        CollectionAssert.AreEqual(
            new[] { "ack:10001:101:18" },
            calls);
    }

    [Test]
    public void CompleteDecisionInvokesCompleteWhenAllowed()
    {
        var calls = new List<string>();
        var executor = CreateExecutor(calls, true, new ReconnectSessionInfo());

        executor.Handle(new ReconnectCatchUpDecision(ReconnectCatchUpAction.Complete, 30));

        CollectionAssert.AreEqual(
            new[] { "complete" },
            calls);
    }

    [Test]
    public void NoneDecisionDoesNothing()
    {
        var calls = new List<string>();
        var executor = CreateExecutor(calls, true, new ReconnectSessionInfo());

        executor.Handle(ReconnectCatchUpDecision.None);

        Assert.IsEmpty(calls);
    }

    [Test]
    public void DecisionDoesNothingWhenBlocked()
    {
        var calls = new List<string>();
        var sessionInfo = new ReconnectSessionInfo
        {
            RoomId = 10001,
            PlayerId = 101,
        };
        var executor = CreateExecutor(calls, false, sessionInfo);

        executor.Handle(new ReconnectCatchUpDecision(ReconnectCatchUpAction.SendAck, 18));

        Assert.IsEmpty(calls);
    }

    private static BattleReconnectDecisionExecutor CreateExecutor(
        List<string> calls,
        bool canHandleDecision,
        ReconnectSessionInfo sessionInfo)
    {
        return new BattleReconnectDecisionExecutor(
            () => canHandleDecision,
            () => sessionInfo,
            (roomId, playerId, frame) => calls.Add($"ack:{roomId}:{playerId}:{frame}"),
            () => calls.Add("complete"));
    }
}
