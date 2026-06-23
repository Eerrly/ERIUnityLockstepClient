using System;
using UnityEngine;

/// <summary>
/// 管理断线重连数据和进度。Phase 1 先承接纯状态计算，Coroutine 和 UI 通知仍由 GameManager 驱动。
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

    public string BuildProgressText(int currentFrame, int targetFrame)
    {
        if (targetFrame <= 0)
            return "目标帧 0";

        return $"同步帧 {Math.Max(0, Math.Min(currentFrame, targetFrame))}/{targetFrame}";
    }

    public float CalculateProgress01(int currentFrame, int targetFrame)
    {
        if (targetFrame <= 0)
            return 1f;

        return Mathf.Clamp01(Math.Max(0, currentFrame) / (float)targetFrame);
    }
}
