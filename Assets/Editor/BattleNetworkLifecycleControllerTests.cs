using System.Collections.Generic;
using NUnit.Framework;

public class BattleNetworkLifecycleControllerTests
{
    [Test]
    public void StartBattleConnectionConnectsThenUpdates()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StartBattleConnection();

        CollectionAssert.AreEqual(new[] { "connect", "update" }, calls);
    }

    [Test]
    public void StopBattleConnectionShutsDown()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StopBattleConnection();

        CollectionAssert.AreEqual(new[] { "shutdown" }, calls);
    }

    [Test]
    public void StartThenStopKeepsNetworkLifecycleOrder()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StartBattleConnection();
        controller.StopBattleConnection();

        CollectionAssert.AreEqual(new[] { "connect", "update", "shutdown" }, calls);
    }

    private static BattleNetworkLifecycleController CreateController(List<string> calls)
    {
        return new BattleNetworkLifecycleController(
            () => calls.Add("connect"),
            () => calls.Add("update"),
            () => calls.Add("shutdown"));
    }
}
