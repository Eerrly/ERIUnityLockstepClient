using System;

public class BattleReconnectDecisionExecutor
{
    private readonly Func<bool> _canHandleDecision;
    private readonly Func<ReconnectSessionInfo> _getSessionInfo;
    private readonly Action<uint, uint, int> _sendReconnectAck;
    private readonly Action _completeReconnect;

    public BattleReconnectDecisionExecutor(
        Func<bool> canHandleDecision,
        Func<ReconnectSessionInfo> getSessionInfo,
        Action completeReconnect)
        : this(
            canHandleDecision,
            getSessionInfo,
            (roomId, playerId, targetFrame) => NetworkManager.Instance.SendBattleReconnectMessage(roomId, playerId, targetFrame),
            completeReconnect)
    {
    }

    public BattleReconnectDecisionExecutor(
        Func<bool> canHandleDecision,
        Func<ReconnectSessionInfo> getSessionInfo,
        Action<uint, uint, int> sendReconnectAck,
        Action completeReconnect)
    {
        _canHandleDecision = canHandleDecision ?? throw new ArgumentNullException(nameof(canHandleDecision));
        _getSessionInfo = getSessionInfo ?? throw new ArgumentNullException(nameof(getSessionInfo));
        _sendReconnectAck = sendReconnectAck ?? throw new ArgumentNullException(nameof(sendReconnectAck));
        _completeReconnect = completeReconnect ?? throw new ArgumentNullException(nameof(completeReconnect));
    }

    public void Handle(ReconnectCatchUpDecision decision)
    {
        if (!_canHandleDecision())
            return;

        var sessionInfo = _getSessionInfo();
        if (sessionInfo == null)
            return;

        switch (decision.Action)
        {
            case ReconnectCatchUpAction.SendAck:
                _sendReconnectAck(sessionInfo.RoomId, sessionInfo.PlayerId, decision.TargetFrame);
                break;
            case ReconnectCatchUpAction.Complete:
                _completeReconnect();
                break;
        }
    }
}
