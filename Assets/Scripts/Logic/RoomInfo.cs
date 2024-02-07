using System.Collections.Generic;

public class RoomInfo
{

    /// <summary>
    /// 房间ID
    /// </summary>
    public uint ID = 0;

    /// <summary>
    /// 房间里的玩家ID集合
    /// </summary>
    public List<uint> PlayerIds = new List<uint>(BattleSetting.MaxPlayerInRoomCount);

    /// <summary>
    /// 房间里的玩家准备情况
    /// </summary>
    public List<uint> Status = new List<uint>(BattleSetting.MaxPlayerInRoomCount);

}