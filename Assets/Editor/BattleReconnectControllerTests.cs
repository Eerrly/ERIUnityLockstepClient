using NUnit.Framework;

public class BattleReconnectControllerTests
{
    [Test]
    public void ApplyReconnectAcceptedReturnsAckWhenCatchUpRoundCompletedAndFramesCaughtUp()
    {
        var controller = new BattleReconnectController();
        controller.SessionInfo.RoomId = 10001;
        controller.SessionInfo.PlayerId = 101;
        controller.SessionInfo.AuthoritativeFrame = 20;
        controller.SessionInfo.LastReceivedFrame = 12;

        var decision = controller.ApplyReconnectAccepted(12, true, false);

        Assert.AreEqual(ReconnectCatchUpAction.SendAck, decision.Action);
        Assert.AreEqual(12, decision.TargetFrame);
        Assert.AreEqual(12, controller.SessionInfo.AuthoritativeFrame);
        Assert.AreEqual(12, controller.SessionInfo.LastAckedFrame);
        Assert.IsFalse(controller.SessionInfo.IsCatchUpRoundComplete);
        Assert.AreEqual(ReconnectLoadingStatus.SyncingBattleProgress, controller.Status);
        Assert.AreEqual("等待服务器确认同步", controller.ProgressText);
        Assert.AreEqual(1f, controller.Progress01);
    }

    [Test]
    public void ApplyReconnectAcceptedReturnsCompleteWhenReconnectCompleteAndFramesCaughtUp()
    {
        var controller = new BattleReconnectController();
        controller.SessionInfo.LastReceivedFrame = 18;

        var decision = controller.ApplyReconnectAccepted(18, false, true);

        Assert.AreEqual(ReconnectCatchUpAction.Complete, decision.Action);
        Assert.AreEqual(18, decision.TargetFrame);
        Assert.IsTrue(controller.SessionInfo.IsReconnectComplete);
        Assert.AreEqual(ReconnectLoadingStatus.SyncingBattleProgress, controller.Status);
        Assert.AreEqual(1f, controller.Progress01);
    }

    [Test]
    public void ApplyFrameSyncedReturnsCompleteWhenFinalReconnectAlreadyAccepted()
    {
        var controller = new BattleReconnectController();
        controller.SessionInfo.AuthoritativeFrame = 30;
        controller.SessionInfo.LastReceivedFrame = 20;
        controller.SessionInfo.IsReconnectComplete = true;

        var decision = controller.ApplyFrameSynced(30);

        Assert.AreEqual(ReconnectCatchUpAction.Complete, decision.Action);
        Assert.AreEqual(30, decision.TargetFrame);
        Assert.AreEqual(30, controller.SessionInfo.LastReceivedFrame);
        Assert.AreEqual(30, controller.SessionInfo.AuthoritativeFrame);
        Assert.AreEqual(1f, controller.Progress01);
    }

    [Test]
    public void MarkFailedStoresFallbackReasonAndFailedStatus()
    {
        var controller = new BattleReconnectController();

        controller.MarkFailed(null);

        Assert.AreEqual("重连失败", controller.SessionInfo.FailureReason);
        Assert.AreEqual(ReconnectLoadingStatus.Failed, controller.Status);
        Assert.AreEqual("重连失败", controller.ProgressText);
        Assert.AreEqual(0f, controller.Progress01);
    }
}
