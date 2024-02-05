public class BattleNetController
{
    private readonly GameManager _gameManager;
    private uint _frameDataCurrBeingSent;

    public BattleNetController(GameManager gameManager)
    {
        _gameManager = gameManager;
    }
        
    public void SendHeartBeatMsg(uint playerId)
    {
        var c2SMsg = new pb.C2S_HeartbeatMsg()
        {
            PlayerId = playerId,
            TimeStamp = (ulong)_gameManager.BattleController.GetClientStopwatchElapsedMilliseconds(),
        };
        NetworkManager.Instance.SendKcpMsg(pb.BattleMsgID.BattleMsgHeartbeat, c2SMsg);
    }
    
    public void SendReadyMsg(uint playerId, uint roomId)
    {
        var c2SMsg = new pb.C2S_ReadyMsg()
        {
            PlayerId = playerId,
            RoomId = roomId,
        };
        NetworkManager.Instance.SendKcpMsg(pb.BattleMsgID.BattleMsgReady, c2SMsg);
    }
    
    public void SendFrameMsg(uint frame, int data)
    {
        if (_frameDataCurrBeingSent == frame) return;
        
        _frameDataCurrBeingSent = frame;
        var c2SMsg = new pb.C2S_FrameMsg()
        {
            Frame = frame,
            Data = data,
        };
        NetworkManager.Instance.SendKcpMsg(pb.BattleMsgID.BattleMsgFrame, c2SMsg);
    }
        
}