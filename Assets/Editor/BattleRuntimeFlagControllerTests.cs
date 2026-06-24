using System.Collections.Generic;
using NUnit.Framework;

public class BattleRuntimeFlagControllerTests
{
    [Test]
    public void MarkRemoteBattleStoppedClearsConnectedAndStartedWithoutTouchingFrame()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.MarkRemoteBattleStopped();

        CollectionAssert.AreEqual(
            new[]
            {
                "connected:False",
                "started:False"
            },
            calls);
    }

    [Test]
    public void ResetBattleRuntimeClearsConnectedStartedAndFrame()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.ResetBattleRuntime();

        CollectionAssert.AreEqual(
            new[]
            {
                "connected:False",
                "started:False",
                "frame:-1"
            },
            calls);
    }

    [Test]
    public void MarkReconnectCompletedStartsConnectsAndStoresAuthorityFrame()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.MarkReconnectCompleted(33);

        CollectionAssert.AreEqual(
            new[]
            {
                "started:True",
                "connected:True",
                "frame:33"
            },
            calls);
    }

    [Test]
    public void NetworkSettersForwardSingleRuntimeValues()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.SetBattleConnected(true);
        controller.SetBattleStarted(true);
        controller.SetServerAuthorityFrame(12);

        CollectionAssert.AreEqual(
            new[]
            {
                "connected:True",
                "started:True",
                "frame:12"
            },
            calls);
    }

    private static BattleRuntimeFlagController CreateController(List<string> calls)
    {
        return new BattleRuntimeFlagController(
            value => calls.Add($"connected:{value}"),
            value => calls.Add($"started:{value}"),
            value => calls.Add($"frame:{value}"));
    }
}
