using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 游戏管理器
/// </summary>
public class GameManager : AManager<GameManager>
{
    /// <summary>
    /// 房间最大人数
    /// </summary>
    private const int RoomMaxPlayerCount = 2;
    /// <summary>
    /// 战斗是否连接
    /// </summary>
    public bool IsBattleConnected = false;
    /// <summary>
    /// 战斗开始标记
    /// </summary>
    public bool IsBattleStart = false;
    /// <summary>
    /// 账户
    /// </summary>
    public string Account { get; set; }
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }
    /// <summary>
    /// 玩家ID
    /// </summary>
    public uint PlayerId { get; set; }
    /// <summary>
    /// 玩家ID
    /// </summary>
    public uint RoomId { get; set; }
    /// <summary>
    /// 当前接受到的服务器最新的权威帧号
    /// </summary>
    public int ServerAuthorityFrame = -1;

    private LogicNetController _logicNetController;
    /// <summary>
    /// 逻辑网络控制器
    /// </summary>
    public LogicNetController LogicNetController => _logicNetController ?? (_logicNetController = new LogicNetController(Instance));

    private BattleController _battleController;
    /// <summary>
    /// 战斗控制器
    /// </summary>
    public BattleController BattleController => _battleController ?? (_battleController = new BattleController(Instance));
    private BattleNetController _battleNetController;
    /// <summary>
    /// 战斗网络控制器
    /// </summary>
    public BattleNetController BattleNetController => _battleNetController ?? (_battleNetController = new BattleNetController(Instance));
    /// <summary>
    /// 所有的房间ID集合
    /// </summary>
    public List<uint> RoomIdList;
    /// <summary>
    /// 所有房间的信息
    /// </summary>
    public Dictionary<uint, List<uint>> RoomInfoDic;
    /// <summary>
    /// 所有玩家的准备情况
    /// </summary>
    public List<uint> Status;
    /// <summary>
    /// 服务器返回的全部玩家的帧数据
    /// </summary>
    public Dictionary<int, List<int>> FrameInfoDic;
    /// <summary>
    /// 客户端帧数据
    /// </summary>
    public int[] Input;

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        RoomIdList = new List<uint>();
        RoomInfoDic = new Dictionary<uint, List<uint>>();
        Status = new List<uint>();
        FrameInfoDic = new Dictionary<int, List<int>>();
        Input = new int[10000];
    }

    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        BattleController.CancelBattleThread();
        
        RoomIdList.Clear();
        RoomInfoDic.Clear();
        Status.Clear();
        FrameInfoDic.Clear();
    }

    public int GetRoomPos()
    {
        var playerIds = RoomInfoDic[RoomId];
        return playerIds.IndexOf(PlayerId);
    }

    /// <summary>
    /// 登录
    /// </summary>
    public void Login()
    {
        LogicNetController.SendLoginMsg();
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public void CreateRoom()
    {
        LogicNetController.SendCreateRoomMsg();
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public void JoinRoom()
    {
        LogicNetController.SendJoinRoomMsg(RoomIdList.First());
    }

    /// <summary>
    /// 刷新房间信息
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="players"></param>
    public void RefreshRoomInfo(uint roomId, List<uint> players)
    {
        RoomInfoDic[roomId] = players;
        if (RoomInfoDic[roomId].Count == RoomMaxPlayerCount) NetworkManager.Instance.KcpConnect();
    }

    /// <summary>
    /// 开启客户端战斗线程任务
    /// </summary>
    public void StartClientBattleThread()
    {
        BattleController.StartBattleThread();
    }

    /// <summary>
    /// 开始客户端时间计时
    /// </summary>
    public void StartClientBattleStopwatch()
    {
        BattleController.StartClientStopwatch();
    }

    /// <summary>
    /// 房间准备
    /// </summary>
    public void RoomReady()
    {
        BattleNetController.SendReadyMsg(PlayerId, RoomId);
    }

    /// <summary>
    /// 设置帧数据
    /// </summary>
    /// <param name="data">帧数据</param>
    public void SetFrame(int data)
    {
        Input[BattleController.PredictEntity.Frame + 1] = data;
        Logger.Log(LogLevel.Info, $"{PlayerId} SetFrame [{BattleController.PredictEntity.Frame + 1}]->{data}");
    }
    
}