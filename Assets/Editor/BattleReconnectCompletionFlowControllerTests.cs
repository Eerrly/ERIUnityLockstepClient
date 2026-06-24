using System;
using System.Collections.Generic;
using NUnit.Framework;

public class BattleReconnectCompletionFlowControllerTests
{
    [Test]
    public void CompleteRunsSuccessfulCatchUpSequence()
    {
        var calls = new List<string>();
        var controller = CreateController(calls, true, 44, frame => true);

        controller.Complete();

        CollectionAssert.AreEqual(
            new[]
            {
                "begin",
                "setFrameInterval:66",
                "fastForward:44",
                "enterRemote",
                "runtimeComplete:44",
                "align:44",
                "reconnectComplete:44",
                "applyCamera",
                "initView",
                "showRoot",
                "startRemote",
                "finish"
            },
            calls);
    }

    [Test]
    public void CompleteFailsWhenFastForwardReturnsFalse()
    {
        var calls = new List<string>();
        var controller = CreateController(calls, true, 44, frame => false);

        controller.Complete();

        CollectionAssert.AreEqual(
            new[]
            {
                "begin",
                "setFrameInterval:66",
                "fastForward:44",
                "fail:追帧失败"
            },
            calls);
    }

    [Test]
    public void CompleteLogsAndFailsWhenFastForwardThrows()
    {
        var calls = new List<string>();
        var controller = CreateController(
            calls,
            true,
            44,
            frame => throw new InvalidOperationException("boom"));

        controller.Complete();

        CollectionAssert.AreEqual(
            new[]
            {
                "begin",
                "setFrameInterval:66",
                "fastForward:44",
                "log:InvalidOperationException",
                "fail:追帧失败"
            },
            calls);
    }

    [Test]
    public void CompleteDoesNothingWhenBlocked()
    {
        var calls = new List<string>();
        var controller = CreateController(calls, false, 44, frame => true);

        controller.Complete();

        Assert.IsEmpty(calls);
    }

    private static BattleReconnectCompletionFlowController CreateController(
        List<string> calls,
        bool canComplete,
        int targetFrame,
        Func<int, bool> fastForwardToFrame)
    {
        return new BattleReconnectCompletionFlowController(
            66,
            new BattleReconnectCompletionFlowActions
            {
                CanComplete = () => canComplete,
                BeginCompletion = () => calls.Add("begin"),
                GetTargetFrame = () => targetFrame,
                SetFrameInterval = interval => calls.Add($"setFrameInterval:{interval}"),
                FastForwardToFrame = frame =>
                {
                    calls.Add($"fastForward:{frame}");
                    return fastForwardToFrame(frame);
                },
                LogFastForwardException = exception => calls.Add($"log:{exception.GetType().Name}"),
                FailReconnect = reason => calls.Add($"fail:{reason}"),
                EnterRemoteBattleState = () => calls.Add("enterRemote"),
                MarkRuntimeReconnectCompleted = frame => calls.Add($"runtimeComplete:{frame}"),
                AlignServerTimeToFrame = frame => calls.Add($"align:{frame}"),
                MarkReconnectCompleted = frame => calls.Add($"reconnectComplete:{frame}"),
                ApplyBattleCamera = () => calls.Add("applyCamera"),
                InitBattleView = () => calls.Add("initView"),
                ShowBattleRoot = () => calls.Add("showRoot"),
                StartRemoteBattle = () => calls.Add("startRemote"),
                FinishCompletion = () => calls.Add("finish")
            });
    }
}
