using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    /// 当前接受到的服务器最新的权威帧号
    /// </summary>
    public int ServerAuthorityFrame = -1;
    /// <summary>
    /// 逻辑网络控制器
    /// </summary>
    private LogicNetController _logicNetController;
    /// <summary>
    /// 战斗控制器
    /// </summary>
    private BattleController _battleController;
    /// <summary>
    /// 战斗网络控制器
    /// </summary>
    private BattleNetController _battleNetController;
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
    /// 战斗逻辑与网络引擎
    /// </summary>
    private FrameEngine _frameEngine;
    /// <summary>
    /// 当前玩家实例
    /// </summary>
    public PlayerInfo Player;

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
        Player = new PlayerInfo();

        _logicNetController = new LogicNetController();
        _battleNetController = new BattleNetController();
        _battleController = new BattleController();
        
        _frameEngine = new FrameEngine();
        _frameEngine.RegisterFrameUpdateListener(_battleController.LogicUpdate);
        _frameEngine.RegisterNetUpdateListener(_battleController.NetUpdate);
    }

    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        _frameEngine.UnRegisterFrameUpdateListener();
        _frameEngine.UnRegisterNetUpdateListener();
        _frameEngine.StopEngine();
        
        RoomIdList.Clear();
        RoomInfoDic.Clear();
        Status.Clear();
        FrameInfoDic.Clear();
    }

    public int GetRoomPos()
    {
        var playerIds = RoomInfoDic[Player.RoomId];
        return playerIds.IndexOf(Player.PlayerId);
    }

    /// <summary>
    /// 登录
    /// </summary>
    public void Login()
    {
        _logicNetController.SendLoginMsg();
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public void CreateRoom()
    {
        _logicNetController.SendCreateRoomMsg();
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public void JoinRoom()
    {
        _logicNetController.SendJoinRoomMsg(RoomIdList.First());
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

    public void StartBattle()
    {
        _battleController.StartClientStopwatch();
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
    }

    /// <summary>
    /// 房间准备
    /// </summary>
    public void RoomReady()
    {
        _battleNetController.SendReadyMsg(Player.PlayerId, Player.RoomId);
    }

    /// <summary>
    /// Ping消息
    /// </summary>
    public void Heartbeat()
    {
        _battleNetController.SendHeartBeatMsg(Player.PlayerId);
    }

    /// <summary>
    /// 发送帧数据
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="data">帧数据</param>
    public void SendFrame(uint frame, int data)
    {
        _battleNetController.SendFrameMsg(frame, data);
    }

    /// <summary>
    /// 获取客户端时间
    /// </summary>
    /// <returns>客户端流逝的时间</returns>
    public long GetClientStopwatchElapsedMilliseconds()
    {
        return _battleController.GetClientStopwatchElapsedMilliseconds();
    }

    /// <summary>
    /// 获取显示实体
    /// </summary>
    /// <returns>显示实体</returns>
    public Entity GetDisplayEntity()
    {
        return _battleController.DisplayEntity;
    }

    /// <summary>
    /// 设置帧数据
    /// </summary>
    /// <param name="data">帧数据</param>
    public void SetFrame(int data)
    {
        Input[_battleController.PredictEntity.Frame + 1] = data;
        Logger.Log(LogLevel.Info, $"{Player.PlayerId} SetFrame [{_battleController.PredictEntity.Frame + 1}]->{data}");
    }
    
}