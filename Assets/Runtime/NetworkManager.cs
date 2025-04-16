using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Google.Protobuf;

/// <summary>
/// 网络管理器
/// </summary>
public class NetworkManager : AManager<NetworkManager>
{
    private Stopwatch _serverStopwatch;
    private KcpClientTransport _kcpClientTransport;
    private TcpClientTransport _tcpClientTransport;
    private MemoryStream _memoryStream;
    /// <summary>
    /// 发送帧字节数组
    /// </summary>
    private readonly byte[] _sendFrameByteArray = new byte[1];

    /// <summary>
    /// KCP是否连接
    /// </summary>
    public bool KcpConnected => _kcpClientTransport.Connected;
    /// <summary>
    /// TCP是否连接
    /// </summary>
    public bool TcpConnected => _tcpClientTransport.Connected;

    /// <summary>
    /// KCP地址信息
    /// </summary>
    public Uri KcpUri => _kcpClientTransport.Uri();
    /// <summary>
    /// TCP地址信息
    /// </summary>
    public Uri TcpUri => _tcpClientTransport.Uri();

    /// <summary>
    /// 最小Ping值
    /// </summary>
    public long MinPing;
    /// <summary>
    /// 当前Ping值
    /// </summary>
    public long RealPing;

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        _serverStopwatch = new Stopwatch();
        _memoryStream = new MemoryStream();
        _kcpClientTransport = new KcpClientTransport(KcpUtil.defaultConfig, NetSetting.KcpPort)
        {
            OnConnected = OnKcpConnected,
            OnDataReceived = OnKcpDataReceived,
            OnDisconnected = OnKcpDisconnected,
            OnError = OnKcpError
        };
        _tcpClientTransport = new TcpClientTransport(NetSetting.TcpPort)
        {
            OnDataReceived = OnTcpDataReceived
        };
    }

    /// <summary>
    /// KCP连接回调
    /// </summary>
    private void OnKcpConnected()
    {
        Logger.Log(LogLevel.Info,$"OnKcpConnected");
    }

    /// <summary>
    /// 收到KCP服务器消息回调处理函数
    /// </summary>
    /// <param name="data">数据字节数组</param>
    /// <param name="channel">消息类型</param>
    private void OnKcpDataReceived(ArraySegment<byte> data, kcp2k.KcpChannel channel)
    {
        if (!KcpConnected)
        {
            Logger.Log(LogLevel.Error, $"[KCP] Kcp Not Connected!");
            return;
        }
        Logger.Log(LogLevel.Info,$"[KCP] OnKcpDataReceived data.len: {data.Count} channel: {channel}");
        if (data.Array == null) {
            Logger.Log(LogLevel.Error,$"[KCP] OnKcpDataReceived data.Array == null");
            return;
        }

        var gameManager = GameManager.Instance;
        _kcpClientTransport.OnMessageProcess(data.ToArray(), _memoryStream, cmd => {
            Logger.Log(LogLevel.Info,$"[KCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Count} Channel:{Enum.GetName(typeof(kcp2k.KcpChannel), channel)}");
            switch (cmd)
            {
                case (byte)pb.BattleMsgID.BattleMsgConnect:
                {
                    var s2CMessage = pb.S2C_ConnectMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgConnect -> errorCode:{s2CMessage.ErrorCode}");

                    gameManager.IsBattleConnected = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReady:
                {
                    var s2CMessage = pb.S2C_ReadyMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgReady -> errorCoe:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} status:{s2CMessage.Status}");

                    if(gameManager.IsBattleConnected && gameManager.RoomInfo.RoomId == s2CMessage.RoomId)
                        gameManager.RoomInfo.Readies = s2CMessage.Status.ToList();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgStart:
                {
                    var s2CMessage = pb.S2C_StartMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgStart -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} timestamp:{s2CMessage.TimeStamp}");

                    _serverStopwatch.Start();
                    gameManager.IsBattleStart = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    if (gameManager.IsBattleStart)
                        gameManager.StartBattle(BattleType.Remote);
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgHeartbeat:
                {
                    var s2CMessage = pb.S2C_HeartbeatMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgHeartbeat -> errorCode:{s2CMessage.ErrorCode} timestamp:{s2CMessage.TimeStamp}");

                    var ping = _serverStopwatch.ElapsedMilliseconds - (long)s2CMessage.TimeStamp;
                    // 通过客户端模拟服务器当前时间戳 - 服务器返回心跳时的当前时间戳 得到一个Ping值
                    RealPing = ping;
                    MinPing = MinPing == 0 ? ping : Math.Min(MinPing, RealPing);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgHeartbeat -> RealPing:{RealPing} MinPing:{MinPing}");
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgFrame:
                {
                    var s2CMessage = pb.S2C_FrameMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} playerCount:{s2CMessage.PlayerCount} inputCount:{s2CMessage.InputCount} datumCount:{s2CMessage.Datum.Count()}");
                    
                    var inputFrame = FrameBuffer.Frame.defFrame;
                    inputFrame.frame = (int)s2CMessage.Frame;
                    inputFrame.playerCount = (int)s2CMessage.PlayerCount;
                    
                    var byteArray = s2CMessage.Datum.ToByteArray();
                    // 默认类型最大值，表示没有操作
                    for (int i = 0; i < inputFrame.playerCount; i++)
                        inputFrame[i] = new FrameBuffer.Input(byte.MaxValue);
                    // 如果有操作，则覆盖真实的帧数据
                    for (int i = 0; i < BattleSetting.MaxPlayerInRoomCount; i++)
                        if ((s2CMessage.InputCount & (1 << i)) == (1 << i)) inputFrame[i] = new FrameBuffer.Input(byteArray[i]);
                    
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame frame:{inputFrame.frame} B0:[{byteArray[0]}] B1:[{byteArray[1]}] D0:[{inputFrame[0]}] D1:[{inputFrame[1]}]");

                    // 帧差
                    var diff = 0;
                    // 出现无法同步帧的情况一般来说是跳帧了，断开KCP连接
                    while (!gameManager.FrameBuffer.SyncFrame(inputFrame.frame, ref inputFrame, ref diff))
                    {
                        Logger.Log(LogLevel.Error,$"[KCP] BattleMsgFrame Can't SyncFrame frame->{inputFrame.frame} diff->{diff}");
                        KcpShutdown();
                        break;
                    }

                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame SyncFrame [frame]->{inputFrame.frame}");
                    // 更新服务器权威帧
                    gameManager.ServerAuthorityFrame = inputFrame.frame;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgCheck:
                {
                    var s2CMessage = pb.S2C_CheckMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgCheck -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame}");
                    
                    if (s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrDiff)
                        Logger.Log(LogLevel.Error, $"[KCP] BattleMsgCheck -> frame:{s2CMessage.Frame} out of sync!");
                    break;
                }
            }
        }, _kcpClientTransport.Shutdown);
    }

    /// <summary>
    /// KCP断开连接时回调
    /// </summary>
    private void OnKcpDisconnected()
    {
        Logger.Log(LogLevel.Info,$"OnKcpDisconnected");
    }

    /// <summary>
    /// KCP发送错误时回调
    /// </summary>
    /// <param name="errorCode"></param>
    /// <param name="error"></param>
    private void OnKcpError(kcp2k.ErrorCode errorCode, string error)
    {
        Logger.Log(LogLevel.Error,$"OnKcpError errorCode: {errorCode} error: {error}");
    }

    /// <summary>
    /// KCP连接
    /// </summary>
    public void KcpConnect()
    {
        _kcpClientTransport.Connect(NetSetting.NetAddress);
    }

    /// <summary>
    /// KCP轮询
    /// </summary>
    public void KcpUpdate()
    {
        _kcpClientTransport.Update();
    }

    /// <summary>
    /// 断开KCP连接
    /// </summary>
    public void KcpShutdown()
    {
        Logger.Log(LogLevel.Info, $"[KCP] {KcpUri} Shutdown!");
        _kcpClientTransport.Shutdown();
    }

    /// <summary>
    /// 发送战斗连接消息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="seasonId">战斗ID</param>
    public void SendBattleConnectMessage(uint playerId, uint seasonId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_ConnectMsg>();
        c2SMessage.PlayerId = playerId;
        c2SMessage.SeasonId = seasonId;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgConnect, c2SMessage);
    }

    /// <summary>
    /// 发送战斗准备消息
    /// </summary>
    /// <param name="roomId">房间ID</param>
    /// <param name="playerId">玩家ID</param>
    public void SendBattleReadyMessage(uint roomId, uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_ReadyMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgReady, c2SMessage);
    }

    /// <summary>
    /// 发送战斗心跳消息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <param name="timestamp">时间戳</param>
    public void SendBattleHeartBeatMessage(uint playerId, ulong timestamp)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_HeartbeatMsg>();
        c2SMessage.PlayerId = playerId;
        c2SMessage.TimeStamp = timestamp;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgHeartbeat, c2SMessage);
    }

    /// <summary>
    /// 发送战斗帧数据消息
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="data">数据体</param>
    public void SendBattleFrameMessage(uint frame, byte data)
    {
        _sendFrameByteArray[0] = data;
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_FrameMsg>(true);
        c2SMessage.Frame = frame;
        // .proto bytes类型 的必要转换
        c2SMessage.Datum = ByteString.CopyFrom(_sendFrameByteArray);
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgFrame, c2SMessage);
    }

    /// <summary>
    /// 发送战斗校验消息
    /// </summary>
    /// <param name="frame">帧号</param>
    /// <param name="pos">战斗POS</param>
    /// <param name="md5">校验码</param>
    public void SendBattleCheckMessage(int frame, int pos, int md5)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_CheckMsg>();
        c2SMessage.Frame = frame;
        c2SMessage.Pos = pos;
        c2SMessage.Md5 = md5;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgCheck, c2SMessage);
    }

    /// <summary>
    /// 收到TCP服务器消息处理回调
    /// </summary>
    /// <param name="data">字节数组</param>
    /// <param name="read">已读索引</param>
    /// <param name="stream">数据流</param>
    private void OnTcpDataReceived(byte[] data, int read, NetworkStream stream)
    {
        if (!TcpConnected)
        {
            Logger.Log(LogLevel.Error, $"[TCP] Tcp Not Connected!");
            return;
        }
        Logger.Log(LogLevel.Info,$"OnTcpDataReceived data.len: {data.Length} read: {read}");
        var gameManager = GameManager.Instance;
        _tcpClientTransport.OnMessageProcess(data, _memoryStream, cmd => {
            Logger.Log(LogLevel.Info,$"[TCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Length}");
            switch (cmd)
            {
                case (byte)pb.LogicMsgID.LogicMsgLogin:
                {
                    var s2CMessage = pb.S2C_LoginMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgLogin -> errorCode:{s2CMessage.ErrorCode} playerId:{s2CMessage.PlayerId}");

                    gameManager.PlayerId = s2CMessage.PlayerId;
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgCreateRoom:
                {
                    var s2CMessage = pb.S2C_CreateRoomMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgCreateRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId}");
                    
                    if (gameManager.RoomInfo == null)
                        gameManager.RoomInfo = new RoomInfo() { RoomId = s2CMessage.RoomId };
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgJoinRoom:
                {
                    var s2CMessage = pb.S2C_JoinRoomMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgJoinRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} all:{s2CMessage.All}");

                    foreach (var playerId in s2CMessage.All)
                    {
                        if (playerId == gameManager.PlayerId) {
                            gameManager.RoomInfo.Gamers.Clear();
                            gameManager.RoomInfo.Gamers.AddRange(s2CMessage.All);
                        }
                    }
                    // 当房间人数到了可以战斗开启的人数时，开启KCP服务器并且开始轮询
                    if (s2CMessage.All.Count == GameSetting.RoomMaxPlayerCount)
                    {
                        // KcpConnect();
                        // KcpUpdate();
                        LoomManager.Instance.QueueOnMainThread(GameManager.Instance.OnRoomFull);
                    }
                    break;
                }
            }
        }, _tcpClientTransport.Shutdown);
    }

    /// <summary>
    /// TCP连接服务器
    /// </summary>
    public void TcpConnect()
    {
        _tcpClientTransport.Connect(NetSetting.NetAddress);
    }

    /// <summary>
    /// 断开TCP连接服务器
    /// </summary>
    public void TcpShutdown()
    {
        Logger.Log(LogLevel.Info, $"[TCP] {TcpUri} Shutdown!");
        _tcpClientTransport.Shutdown();
    }

    /// <summary>
    /// 发送登录消息
    /// </summary>
    /// <param name="account">账号</param>
    /// <param name="password">密码</param>
    public void SendLogicLoginMessage(string account, string password)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_LoginMsg>();
        c2SMessage.Account = ByteString.CopyFromUtf8(account);
        c2SMessage.Password = ByteString.CopyFromUtf8(password);
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgLogin, c2SMessage);
    }

    /// <summary>
    /// 发送创建房间消息
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    public void SendLogicCreateRoomMessage(uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_CreateRoomMsg>();
        c2SMessage.PlayerId = playerId;
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgCreateRoom, c2SMessage);
    }

    /// <summary>
    /// 发送加入房间消息
    /// </summary>
    /// <param name="roomId">房间ID</param>
    /// <param name="playerId">玩家ID</param>
    public void SendLogicJoinRoomMessage(uint roomId, uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_JoinRoomMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgJoinRoom, c2SMessage);
    }
    
    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        KcpShutdown();
        TcpShutdown();
        _serverStopwatch.Stop();
        _memoryStream.Dispose();
    }
}