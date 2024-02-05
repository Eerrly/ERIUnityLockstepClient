using Google.Protobuf;

public class LogicNetController
{
    private readonly GameManager _gameManager;

    public LogicNetController(GameManager gameManager)
    {
        _gameManager = gameManager;
    }

    public void Connect(string ip, int port)
    {
        NetworkManager.Instance.KcpConnect();
    }

    public void SendLoginMsg()
    {
        NetworkManager.Instance.SendTcpMsg(pb.LogicMsgID.LogicMsgLogin, new pb.C2S_LoginMsg
        {
            Account = ByteString.CopyFromUtf8(_gameManager.Account),
            Password = ByteString.CopyFromUtf8(_gameManager.Password)
        });
    }

    public void SendCreateRoomMsg()
    {
        NetworkManager.Instance.SendTcpMsg(pb.LogicMsgID.LogicMsgCreateRoom, new pb.C2S_CreateRoomMsg
        {
            PlayerId = _gameManager.PlayerId,
        });
    }

    public void SendJoinRoomMsg(uint roomId)
    {
        NetworkManager.Instance.SendTcpMsg(pb.LogicMsgID.LogicMsgJoinRoom, new pb.C2S_JoinRoomMsg
        {
            PlayerId = _gameManager.PlayerId,
            RoomId = roomId,
        });
    }

}