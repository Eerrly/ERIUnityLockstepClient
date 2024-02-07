using Google.Protobuf;

/// <summary>
/// 逻辑网络控制器
/// </summary>
public class LogicNetController
{
    public LogicNetController()
    {
    }

    /// <summary>
    /// 发送登录消息
    /// </summary>
    public void SendLoginMsg()
    {
        NetworkManager.Instance.SendTcpMsg(pb.LogicMsgID.LogicMsgLogin, new pb.C2S_LoginMsg
        {
            Account = ByteString.CopyFromUtf8(GameManager.Instance.Player.Account),
            Password = ByteString.CopyFromUtf8(GameManager.Instance.Player.Password)
        });
    }

    /// <summary>
    /// 发送创建房间消息
    /// </summary>
    public void SendCreateRoomMsg()
    {
        NetworkManager.Instance.SendTcpMsg(pb.LogicMsgID.LogicMsgCreateRoom, new pb.C2S_CreateRoomMsg
        {
            PlayerId = GameManager.Instance.Player.PlayerId,
        });
    }

    /// <summary>
    /// 发送加入房间消息
    /// </summary>
    /// <param name="roomId">房间ID</param>
    public void SendJoinRoomMsg(uint roomId)
    {
        NetworkManager.Instance.SendTcpMsg(pb.LogicMsgID.LogicMsgJoinRoom, new pb.C2S_JoinRoomMsg
        {
            PlayerId = GameManager.Instance.Player.PlayerId,
            RoomId = roomId,
        });
    }

}