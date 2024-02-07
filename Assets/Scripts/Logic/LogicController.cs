using System.Collections.Generic;
using System.Linq;

public class LogicController
{
    /// <summary>
    /// 当前玩家实例
    /// </summary>
    public PlayerInfo Player;
    /// <summary>
    /// 所有的房间集合
    /// </summary>
    public List<RoomInfo> Rooms;

    
    public LogicController()
    {
        Rooms = new List<RoomInfo>();
    }
    
    /// <summary>
    /// 获取房间位置
    /// </summary>
    /// <returns></returns>
    public int GetRoomPos()
    {
        if (Player == null) return -1;
        foreach (var room in Rooms.Where(room => room.PlayerIds.Contains(Player.ID)))
            return room.PlayerIds.IndexOf(Player.ID);
        return -1;
    }
    
    /// <summary>
    /// 刷新房间信息
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="players"></param>
    public void RefreshRoomInfo(uint roomId, List<uint> players = null)
    {
        if (players == null)
        {
            Rooms.Add(new RoomInfo()
            {
                ID = roomId,
            });
            return;
        }
        var flag = false;
        foreach (var room in Rooms.Where(room => roomId == room.ID))
        {
            room.PlayerIds = players;
            flag = true;
        }
        if (!flag)
        {
            Rooms.Add(new RoomInfo()
            {
                ID = roomId,
                PlayerIds = players,
            });
        }
        if (players.Contains(Player.ID))
        {
            Player.RoomId = roomId;
        }
        if (players.Count == BattleSetting.MaxPlayerInRoomCount)
        {
            NetworkManager.Instance.KcpConnect();
        }
    }
    
}