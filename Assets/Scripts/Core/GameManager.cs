using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 游戏管理器
/// </summary>
public class GameManager : AManager<GameManager>
{
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
    private LogicController _logicController;
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
    /// 客户端帧数据
    /// </summary>
    public byte[] Input;
    /// <summary>
    /// 战斗逻辑与网络引擎
    /// </summary>
    private FrameEngine _frameEngine;
    
    private FrameBuffer _frameBuffer;
    /// <summary>
    /// 帧数据
    /// </summary>
    public FrameBuffer FrameBuffer
    {
        get => _frameBuffer;
        set => _frameBuffer = value;
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        Input = new byte[BattleSetting.MaxFrameCount];

        _logicController = new LogicController();
        _logicNetController = new LogicNetController();
        _battleNetController = new BattleNetController();
        _battleController = new BattleController();

        _frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
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
        
        Array.Clear(Input, 0, Input.Length);
        Input = null;
    }

    /// <summary>
    /// 获取房间位置
    /// </summary>
    /// <returns></returns>
    public int GetRoomPos()
    {
        return _logicController.GetRoomPos();
    }

    /// <summary>
    /// 登录
    /// </summary>
    public void Login(string account, string password)
    {
        _logicNetController.SendLoginMsg(account, password);
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
        _logicNetController.SendJoinRoomMsg(_logicController.Rooms.First().ID);
    }

    /// <summary>
    /// 刷新房间信息
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="players"></param>
    public void RefreshRoomInfo(uint roomId, List<uint> players = null)
    {
        _logicController.RefreshRoomInfo(roomId, players);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="room"></param>
    /// <returns></returns>
    public bool TryGetRoom(uint roomId, out RoomInfo room)
    {
        room = null;
        var flag = false;
        foreach (var r in _logicController.Rooms.Where(r => r.ID == roomId))
        {
            room = r;
            flag = true;
        }
        return flag;
    }
    
    /// <summary>
    /// 获取房间列表
    /// </summary>
    /// <returns></returns>
    public List<RoomInfo> GetRoomIdList()
    {
        return _logicController.Rooms;
    }

    /// <summary>
    /// 开启战斗
    /// </summary>
    public void StartBattle()
    {
        _battleController.InitEntities();
        _battleController.StartClientStopwatch();
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
    }

    /// <summary>
    /// 房间准备
    /// </summary>
    public void RoomReady()
    {
        if(!IsBattleConnected) return;
        _battleNetController.SendReadyMsg(_logicController.Player.ID, _logicController.Player.RoomId);
    }

    /// <summary>
    /// Ping消息
    /// </summary>
    public void Heartbeat()
    {
        if(!IsBattleConnected) return;
        _battleNetController.SendHeartBeatMsg(_logicController.Player.ID);
    }

    /// <summary>
    /// 发送帧数据
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="input">操作数据</param>
    public void SendFrame(uint frame, FrameBuffer.Input input)
    {
        if(!IsBattleConnected) return;
        _battleNetController.SendFrameMsg(frame, input);
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
    public BattleEntity GetDisplayEntity()
    {
        return _battleController.DisplayEntity;
    }
    
    /// <summary>
    /// 初始化玩家实体
    /// </summary>
    /// <param name="playerId"></param>
    public void InitPlayer(uint playerId)
    {
        _logicController.Player = new PlayerInfo
        {
            ID = playerId
        };
    }

    /// <summary>
    /// 获取玩家实体
    /// </summary>
    /// <returns></returns>
    public PlayerInfo GetPlayer()
    {
        return _logicController.Player;
    }

    /// <summary>
    /// 设置房间玩家状态
    /// </summary>
    /// <param name="roomId"></param>
    /// <param name="status"></param>
    public void SetRoomStatus(uint roomId, List<uint> status)
    {
        foreach (var r in _logicController.Rooms.Where(r => r.ID == roomId))
        {
            r.Status = status;
        }
    }
    
    /// <summary>
    /// 设置帧数据
    /// </summary>
    /// <param name="data">帧数据</param>
    public void SetFrame(byte data)
    {
        Input[_battleController.PredictEntity.Frame + 1] = data;
        Logger.Log(LogLevel.Info, $"{_logicController.Player.ID} SetFrame [{_battleController.PredictEntity.Frame + 1}]->{data}");
    }
    
}