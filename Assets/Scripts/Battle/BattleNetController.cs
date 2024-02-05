/// <summary>
/// 战斗网络控制器
/// </summary>
public class BattleNetController
{
    /// <summary>
    /// 游戏管理器对象
    /// </summary>
    private readonly GameManager _gameManager;
    /// <summary>
    /// 最后一次发送的客户端帧号
    /// </summary>
    private uint _frameDataCurrBeingSent;

    public BattleNetController(GameManager gameManager)
    {
        _gameManager = gameManager;
    }
        
    /// <summary>
    /// 发送Ping消息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void SendHeartBeatMsg(uint playerId)
    {
        var c2SMsg = new pb.C2S_HeartbeatMsg()
        {
            PlayerId = playerId,
            TimeStamp = (ulong)_gameManager.BattleController.GetClientStopwatchElapsedMilliseconds(),
        };
        NetworkManager.Instance.SendKcpMsg(pb.BattleMsgID.BattleMsgHeartbeat, c2SMsg);
    }
    
    /// <summary>
    /// 发送准备消息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="roomId">房间ID</param>
    public void SendReadyMsg(uint playerId, uint roomId)
    {
        var c2SMsg = new pb.C2S_ReadyMsg()
        {
            PlayerId = playerId,
            RoomId = roomId,
        };
        NetworkManager.Instance.SendKcpMsg(pb.BattleMsgID.BattleMsgReady, c2SMsg);
    }
    
    /// <summary>
    /// 发送帧数据消息
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="data">帧数据</param>
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