using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public class BattleReturnToMainFlowControllerTests
{
    [Test]
    public void ExitReplayToMainStopsReplayLoadsMainShowsMainAndTransitionsBeforeComplete()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        RunToEnd(controller.ExitReplayToMain(
            () => calls.Add("stopReplay"),
            () => calls.Add("complete")));

        CollectionAssert.AreEqual(
            new[]
            {
                "stopReplay",
                "load:Main",
                "loaded:Main",
                "showMain",
                "transitionMain",
                "complete"
            },
            calls);
    }

    [Test]
    public void ExitRemoteBattleToMainPublishesDefaultReasonAfterMainLoaded()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        RunToEnd(controller.ExitRemoteBattleToMain(
            () => calls.Add("stopRemote"),
            string.Empty,
            () => calls.Add("complete")));

        CollectionAssert.AreEqual(
            new[]
            {
                "stopRemote",
                "load:Main",
                "loaded:Main",
                "showMain",
                "transitionMain",
                "status:战斗已退出",
                "complete"
            },
            calls);
    }

    [Test]
    public void ReturnToMainAfterReconnectFailureWaitsCleansResetsLoadsMainAndPublishesDefaultReason()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        RunToEnd(controller.ReturnToMainAfterReconnectFailure(
            () => calls.Add("cleanupReconnect"),
            () => calls.Add("resetReconnectRuntime"),
            string.Empty,
            () => calls.Add("complete")));

        CollectionAssert.AreEqual(
            new[]
            {
                "wait:1",
                "waited:1",
                "cleanupReconnect",
                "transitionMain",
                "resetReconnectRuntime",
                "load:Main",
                "loaded:Main",
                "showMain",
                "status:重连失败",
                "complete"
            },
            calls);
    }

    private static BattleReturnToMainFlowController CreateController(List<string> calls)
    {
        return new BattleReturnToMainFlowController(
            onLoaded => LoadMainStub(onLoaded, calls),
            () => calls.Add("showMain"),
            () => calls.Add("transitionMain"),
            message => calls.Add($"status:{message}"),
            seconds => WaitStub(seconds, calls));
    }

    private static IEnumerator LoadMainStub(Action onLoaded, List<string> calls)
    {
        calls.Add("load:Main");
        yield return null;
        calls.Add("loaded:Main");
        onLoaded?.Invoke();
    }

    private static IEnumerator WaitStub(float seconds, List<string> calls)
    {
        calls.Add($"wait:{seconds}");
        yield return null;
        calls.Add($"waited:{seconds}");
    }

    private static void RunToEnd(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is IEnumerator nestedRoutine)
                RunToEnd(nestedRoutine);
        }
    }
}
