using System.Collections.Generic;

/// <summary>
/// 重连会话信息
/// </summary>
public class ReconnectSessionInfo
{
    public uint RoomId;
    public uint PlayerId;
    public int AuthoritativeFrame;
    public int LastReceivedFrame;
    public int PlayerPos;
    public string FailureReason;
    public List<uint> Gamers = new List<uint>();

    public void Reset()
    {
        RoomId = 0;
        PlayerId = 0;
        AuthoritativeFrame = 0;
        LastReceivedFrame = -1;
        PlayerPos = 0;
        FailureReason = string.Empty;
        Gamers.Clear();
    }
}
