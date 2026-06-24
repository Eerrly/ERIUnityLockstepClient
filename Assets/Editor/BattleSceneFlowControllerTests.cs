using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

public class BattleSceneFlowControllerTests
{
    [Test]
    public void LoadWorldSceneRunsLoadBeforeCallback()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        RunToEnd(controller.LoadWorldScene(() => calls.Add("callback")));

        CollectionAssert.AreEqual(
            new[] { "load:World", "loaded:World", "callback" },
            calls);
    }

    [Test]
    public void LoadMainSceneRunsLoadBeforeCallback()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        RunToEnd(controller.LoadMainScene(() => calls.Add("callback")));

        CollectionAssert.AreEqual(
            new[] { "load:Main", "loaded:Main", "callback" },
            calls);
    }

    [Test]
    public void LoadReconnectLoadingSceneRunsLoadBeforeCallback()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        RunToEnd(controller.LoadReconnectLoadingScene(() => calls.Add("callback")));

        CollectionAssert.AreEqual(
            new[] { "load:ReconnectLoading", "loaded:ReconnectLoading", "callback" },
            calls);
    }

    [Test]
    public void ShowMainPresentationAppliesMainCameraBeforeModernRoot()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.ShowMainPresentation();

        CollectionAssert.AreEqual(
            new[] { "camera:main", "root:Modern" },
            calls);
    }

    [Test]
    public void ApplyBattleCameraStateForwardsToCameraManager()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.ApplyBattleCameraState();

        CollectionAssert.AreEqual(new[] { "camera:battle" }, calls);
    }

    [Test]
    public void ApplyReplayCameraStateForwardsToCameraManager()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.ApplyReplayCameraState();

        CollectionAssert.AreEqual(new[] { "camera:replay" }, calls);
    }

    [Test]
    public void ShowBattleRootForwardsToUiRootManager()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.ShowBattleRoot();

        CollectionAssert.AreEqual(new[] { "root:Battle" }, calls);
    }

    [Test]
    public void ShowReplayRootForwardsToUiRootManager()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.ShowReplayRoot();

        CollectionAssert.AreEqual(new[] { "root:Replay" }, calls);
    }

    [Test]
    public void ShowReconnectLoadingRootForwardsToUiRootManager()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.ShowReconnectLoadingRoot();

        CollectionAssert.AreEqual(new[] { "root:ReconnectLoading" }, calls);
    }

    [Test]
    public void HideAllRootsForwardsToUiRootManager()
    {
        var calls = new List<string>();
        var controller = CreateController(
            scene => LoadSceneStub(scene, calls),
            calls);

        controller.HideAllRoots();

        CollectionAssert.AreEqual(new[] { "hideRoots" }, calls);
    }

    private static BattleSceneFlowController CreateController(
        Func<EGameScene, IEnumerator> loadScene,
        List<string> calls)
    {
        return new BattleSceneFlowController(
            loadScene,
            () => calls.Add("camera:main"),
            () => calls.Add("camera:battle"),
            () => calls.Add("camera:replay"),
            rootType => calls.Add($"root:{rootType}"),
            () => calls.Add("hideRoots"));
    }

    private static IEnumerator LoadSceneStub(EGameScene scene, List<string> calls)
    {
        calls.Add($"load:{scene}");
        yield return null;
        calls.Add($"loaded:{scene}");
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
