public class RoomInfo
{
    public uint RoomId;
    public List<uint> Readies;
    public List<uint> Gamers;

    public RoomInfo()
    {
        Readies = new List<uint>();
        Gamers = new List<uint>();
    }
}