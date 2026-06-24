using System.Collections.Generic;
using NUnit.Framework;

public class BattleEngineLifecycleControllerTests
{
    [Test]
    public void StartRemoteBattleEngineStartsNetFrameAndRecordInOrder()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StartRemoteBattle(1);

        CollectionAssert.AreEqual(
            new[] { "startNet:1", "startFrame:33", "startRecord:1" },
            calls);
    }

    [Test]
    public void StartReplayBattleEngineStartsReplayEngine()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StartReplayBattle();

        CollectionAssert.AreEqual(new[] { "startReplay:33" }, calls);
    }

    [Test]
    public void StopRemoteBattleEngineStopsEngineAndRecordInOrder()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StopRemoteBattle();

        CollectionAssert.AreEqual(new[] { "stopEngine", "releaseRecord" }, calls);
    }

    [Test]
    public void StopReplayBattleEngineStopsReplayEngine()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        controller.StopReplayBattle();

        CollectionAssert.AreEqual(new[] { "stopReplay" }, calls);
    }

    private static BattleEngineLifecycleController CreateController(List<string> calls)
    {
        return new BattleEngineLifecycleController(
            interval => calls.Add($"startNet:{interval}"),
            interval => calls.Add($"startFrame:{interval}"),
            interval => calls.Add($"startReplay:{interval}"),
            () => calls.Add("stopEngine"),
            () => calls.Add("stopReplay"),
            playerPosition => calls.Add($"startRecord:{playerPosition}"),
            () => calls.Add("releaseRecord"));
    }
}
