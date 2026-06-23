public struct BattleReconnectMessageResult
{
    public bool Success;
    public int AuthoritativeFrame;
    public int PlayerPos;
    public string Reason;
    public bool IsCatchUpRoundComplete;
    public bool IsReconnectComplete;
}

/// <summary>
/// 只负责解释服务端重连回包语义，不直接切场景或改 UI。
/// </summary>
public class BattleReconnectMessageHandler
{
    public BattleReconnectMessageResult Handle(pb.S2C_BattleReconnectMsg message)
    {
        var success = message.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
        var reason = message.Reason;
        if (!success && string.IsNullOrEmpty(reason))
            reason = "重连失败";

        return new BattleReconnectMessageResult
        {
            Success = success,
            AuthoritativeFrame = (int)message.AuthoritativeFrame,
            PlayerPos = (int)message.PlayerPos,
            Reason = reason,
            IsCatchUpRoundComplete = reason == GameManager.ReconnectCatchUpRoundCompleteReason,
            IsReconnectComplete = reason == GameManager.ReconnectCompleteReason
        };
    }
}
