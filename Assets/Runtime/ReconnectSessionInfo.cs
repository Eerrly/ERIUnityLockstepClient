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
    public bool IsCatchUpRoundComplete;
    public bool IsReconnectComplete;
    public int LastAckedFrame = -1;
    public string FailureReason;
    public List<uint> Gamers = new List<uint>();

    public void Reset()
    {
        RoomId = 0;
        PlayerId = 0;
        AuthoritativeFrame = 0;
        LastReceivedFrame = -1;
        PlayerPos = 0;
        IsCatchUpRoundComplete = false;
        IsReconnectComplete = false;
        LastAckedFrame = -1;
        FailureReason = string.Empty;
        Gamers.Clear();
    }
}
