using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public class BattleReconnectFlowControllerTests
{
    [Test]
    public void EnterReconnectBattleSceneRunsLoadingWorldAndConnectionInOrder()
    {
        var calls = new List<string>();
        var controller = CreateController(calls);

        RunToEnd(controller.EnterReconnectBattleScene());

        CollectionAssert.AreEqual(
            new[]
            {
                "hideRoots",
                "load:ReconnectLoading",
                "loaded:ReconnectLoading",
                "showReconnectRoot",
                "status:LoadingBattleScene:加载战斗场景:0",
                "load:World",
                "loaded:World",
                "recreateView",
                "applyBattleCamera",
                "initEntities",
                "initEntitySystems",
                "status:ConnectingServer:连接服务器:0",
                "startConnection"
            },
            calls);
    }

    private static BattleReconnectFlowController CreateController(List<string> calls)
    {
        return new BattleReconnectFlowController(
            () => calls.Add("hideRoots"),
            onLoaded => LoadSceneStub("ReconnectLoading", onLoaded, calls),
            onLoaded => LoadSceneStub("World", onLoaded, calls),
            () => calls.Add("showReconnectRoot"),
            () => calls.Add("recreateView"),
            () => calls.Add("applyBattleCamera"),
            () => calls.Add("initEntities"),
            () => calls.Add("initEntitySystems"),
            () => calls.Add("startConnection"),
            (status, text, progress) => calls.Add($"status:{status}:{text}:{progress}"));
    }

    private static IEnumerator LoadSceneStub(string scene, Action onLoaded, List<string> calls)
    {
        calls.Add($"load:{scene}");
        yield return null;
        calls.Add($"loaded:{scene}");
        onLoaded?.Invoke();
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
