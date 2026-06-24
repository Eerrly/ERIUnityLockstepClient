using System;
using UnityEngine;

public enum ReconnectCatchUpAction
{
    None,
    SendAck,
    Complete,
}

public struct ReconnectCatchUpDecision
{
    public ReconnectCatchUpAction Action;
    public int TargetFrame;

    public ReconnectCatchUpDecision(ReconnectCatchUpAction action, int targetFrame)
    {
        Action = action;
        TargetFrame = targetFrame;
    }

    public static ReconnectCatchUpDecision None => new ReconnectCatchUpDecision(ReconnectCatchUpAction.None, 0);
}

/// <summary>
/// 管理断线重连数据、进度和补帧决策。Coroutine、场景和网络发送仍由 GameManager 驱动。
/// </summary>
public class BattleReconnectController
{
    public ReconnectSessionInfo SessionInfo { get; } = new ReconnectSessionInfo();
    public ReconnectLoadingStatus Status { get; private set; } = ReconnectLoadingStatus.None;
    public string ProgressText { get; private set; } = string.Empty;
    public float Progress01 { get; private set; }

    public void ResetFromLogin(pb.S2C_LoginMsg loginMsg, uint playerId)
    {
        SessionInfo.Reset();
        SessionInfo.RoomId = loginMsg.ReconnectRoomId;
        SessionInfo.PlayerId = playerId;
        SessionInfo.AuthoritativeFrame = (int)loginMsg.ReconnectFrame;
        SessionInfo.LastReceivedFrame = -1;
        SessionInfo.PlayerPos = (int)loginMsg.ReconnectPlayerPos;
        foreach (var gamer in loginMsg.ReconnectGamers)
            SessionInfo.Gamers.Add(gamer);
    }

    public void UpdateStatus(ReconnectLoadingStatus status, string progressText, float progress01)
    {
        Status = status;
        ProgressText = progressText ?? string.Empty;
        Progress01 = Mathf.Clamp01(progress01);
    }

    public void MarkRequestingReconnect(int lastReceivedFrame)
    {
        UpdateStatus(
            ReconnectLoadingStatus.RequestingReconnect,
            BuildProgressText(lastReceivedFrame, SessionInfo.AuthoritativeFrame),
            0f);
    }

    public ReconnectCatchUpDecision ApplyReconnectAccepted(
        int authoritativeFrame,
        bool isCatchUpRoundComplete,
        bool isReconnectComplete)
    {
        if (isCatchUpRoundComplete && !isReconnectComplete)
            SessionInfo.AuthoritativeFrame = authoritativeFrame;
        else
            SessionInfo.AuthoritativeFrame = Math.Max(SessionInfo.AuthoritativeFrame, authoritativeFrame);

        SessionInfo.IsCatchUpRoundComplete |= isCatchUpRoundComplete;
        SessionInfo.IsReconnectComplete |= isReconnectComplete;
        UpdateSyncingStatus(SessionInfo.LastReceivedFrame, SessionInfo.AuthoritativeFrame);
        return EvaluateCatchUpDecision();
    }

    public ReconnectCatchUpDecision ApplyFrameSynced(int frame)
    {
        SessionInfo.LastReceivedFrame = Math.Max(SessionInfo.LastReceivedFrame, frame);
        SessionInfo.AuthoritativeFrame = Math.Max(SessionInfo.AuthoritativeFrame, frame);
        UpdateSyncingStatus(SessionInfo.LastReceivedFrame, SessionInfo.AuthoritativeFrame);
        return EvaluateCatchUpDecision();
    }

    public void MarkEnteringBattle()
    {
        UpdateStatus(ReconnectLoadingStatus.EnteringBattle, "同步完成，正在进入战斗", 1f);
    }

    public void MarkFailed(string reason)
    {
        SessionInfo.FailureReason = string.IsNullOrEmpty(reason) ? "重连失败" : reason;
        UpdateStatus(ReconnectLoadingStatus.Failed, SessionInfo.FailureReason, 0f);
    }

    public void MarkCompleted(int targetFrame)
    {
        SessionInfo.LastReceivedFrame = Math.Max(SessionInfo.LastReceivedFrame, targetFrame);
        SessionInfo.FailureReason = string.Empty;
    }

    public string BuildProgressText(int currentFrame, int targetFrame)
    {
        if (targetFrame <= 0)
            return "目标帧:0";

        return $"同步帧:{Math.Max(0, Math.Min(currentFrame, targetFrame))}/{targetFrame}";
    }

    public float CalculateProgress01(int currentFrame, int targetFrame)
    {
        if (targetFrame <= 0)
            return 1f;

        return Mathf.Clamp01(Math.Max(0, currentFrame) / (float)targetFrame);
    }

    private void UpdateSyncingStatus(int currentFrame, int targetFrame)
    {
        UpdateStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            BuildProgressText(currentFrame, targetFrame),
            CalculateProgress01(currentFrame, targetFrame));
    }

    private ReconnectCatchUpDecision EvaluateCatchUpDecision()
    {
        if (!SessionInfo.IsCatchUpRoundComplete && !SessionInfo.IsReconnectComplete)
            return ReconnectCatchUpDecision.None;

        if (SessionInfo.LastReceivedFrame < SessionInfo.AuthoritativeFrame)
            return ReconnectCatchUpDecision.None;

        if (SessionInfo.IsReconnectComplete)
            return new ReconnectCatchUpDecision(ReconnectCatchUpAction.Complete, SessionInfo.AuthoritativeFrame);

        return CreateAckDecision();
    }

    private ReconnectCatchUpDecision CreateAckDecision()
    {
        var targetFrame = SessionInfo.AuthoritativeFrame;
        if (SessionInfo.LastAckedFrame >= targetFrame)
            return ReconnectCatchUpDecision.None;

        SessionInfo.LastAckedFrame = targetFrame;
        SessionInfo.IsCatchUpRoundComplete = false;
        UpdateStatus(
            ReconnectLoadingStatus.SyncingBattleProgress,
            "等待服务器确认同步",
            CalculateProgress01(SessionInfo.LastReceivedFrame, targetFrame));

        return new ReconnectCatchUpDecision(ReconnectCatchUpAction.SendAck, targetFrame);
    }
}
