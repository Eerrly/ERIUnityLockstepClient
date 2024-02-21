using System.IO;

/// <summary>
/// 战斗网络控制器
/// </summary>
public class BattleNetController
{
    /// <summary>
    /// 最后一次发送的客户端帧号
    /// </summary>
    private uint _frameDataCurrBeingSent;

    private MemoryStream _memoryStream;
    private BinaryWriter _binaryWriter;

    public BattleNetController()
    {
        _memoryStream = new MemoryStream();
        _binaryWriter = new BinaryWriter(_memoryStream);
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
            TimeStamp = (ulong)GameManager.Instance.GetClientStopwatchElapsedMilliseconds(),
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
    /// <param name="input">帧数据</param>
    public void SendFrameMsg(uint frame, FrameBuffer.Input input)
    {
        if (_frameDataCurrBeingSent == frame) return;
        
        _memoryStream.Reset();
        _binaryWriter.Write(frame);
        _binaryWriter.Write(input.ToByte());
        NetworkManager.Instance.SendKcpMsg(pb.BattleMsgID.BattleMsgFrame, _memoryStream.GetBuffer());
    }
        
}