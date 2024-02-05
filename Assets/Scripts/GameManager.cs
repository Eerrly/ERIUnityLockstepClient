using System.Collections.Generic;
using System.Linq;

public class GameManager
{
    private static GameManager _instance;
    public static GameManager Instance => _instance ?? (_instance = new GameManager());

    private const int RoomMaxPlayerCount = 2;

    public bool IsBattleStart = false;

    public string Account { get; set; }
    public string Password { get; set; }
    public uint PlayerId { get; set; }
    public uint RoomId { get; set; }
    public uint CurClientFrame { get; set; }
    public uint CurServerFrame { get; set; }

    private LogicNetController _logicNetController;
    public LogicNetController LogicNetController => _logicNetController ?? (_logicNetController = new LogicNetController(Instance));

    private BattleController _battleController;
    public BattleController BattleController => _battleController ?? (_battleController = new BattleController(Instance));
    private BattleNetController _battleNetController;
    public BattleNetController BattleNetController => _battleNetController ?? (_battleNetController = new BattleNetController(Instance));
    
    public readonly List<uint> RoomIdList;
    public readonly Dictionary<uint, List<uint>> RoomInfoDic;

    public List<uint> Status;
    public readonly Dictionary<uint, List<int>> FrameInfoDic;

    public int[] Input;

    private GameManager()
    {
        RoomIdList = new List<uint>();
        RoomInfoDic = new Dictionary<uint, List<uint>>();
        Status = new List<uint>();
        FrameInfoDic = new Dictionary<uint, List<int>>();
        Input = new int[10000];
    }
    
    public void Login()
    {
        LogicNetController.SendLoginMsg();
    }

    public void CreateRoom()
    {
        LogicNetController.SendCreateRoomMsg();
    }

    public void JoinRoom()
    {
        LogicNetController.SendJoinRoomMsg(RoomIdList.First());
    }

    public void RefreshRoomInfo(uint roomId, List<uint> players)
    {
        RoomInfoDic[roomId] = players;
        if (RoomInfoDic[roomId].Count == RoomMaxPlayerCount) NetworkManager.Instance.KcpConnect();
    }

    public void StartClientBattleThread()
    {
        BattleController.StartBattleThread();
    }

    public void StartClientBattleStopwatch()
    {
        BattleController.StartClientStopwatch();
    }

    public void RoomReady()
    {
        BattleNetController.SendReadyMsg(PlayerId, RoomId);
    }

    public void SetFrame(int data)
    {
        Input[CurClientFrame] = data;
        Logger.Log(LogLevel.Info, $"{PlayerId} SetFrame [{CurClientFrame}]->{data}");
    }

    public void OnRelease()
    {
        NetworkManager.Instance.CloseTcpClient();
        BattleController.CancelBattleThread();
        NetworkManager.Instance.CloseKcpClient();
        
        RoomIdList.Clear();
        RoomInfoDic.Clear();
        Status.Clear();
        FrameInfoDic.Clear();
    }
    
}