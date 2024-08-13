using System.Collections.Generic;

/// <summary>
/// 房间信息
/// </summary>
public class RoomInfo
{
    /// <summary>
    /// 房间ID
    /// </summary>
    public uint RoomId;

    /// <summary>
    /// 房间内已准备的玩家ID集合
    /// </summary>
    public List<uint> Readies;

    /// <summary>
    /// 玩家内所有玩家的ID集合
    /// </summary>
    public List<uint> Gamers;

    public RoomInfo()
    {
        Readies = new List<uint>();
        Gamers = new List<uint>();
    }
}