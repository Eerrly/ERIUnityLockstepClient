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
    /// 最小 Ping
    /// </summary>
    public long MinPing;

    /// <summary>
    /// 当前 Ping
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
    /// KCP 连接回调
    /// </summary>
    private void OnKcpConnected()
    {
        Logger.Log(LogLevel.Info, "OnKcpConnected");
        var gameManager = GameManager.Instance;
        if (gameManager.IsReconnecting)
        {
            SendBattleReconnectMessage(
                gameManager.GetReconnectRoomId(),
                gameManager.GetReconnectPlayerId(),
                gameManager.GetReconnectLastReceivedFrame());
            LoomManager.Instance.QueueOnMainThread(gameManager.OnReconnectRequestSent);
        }
    }

    /// <summary>
    /// 收到 KCP 消息
    /// </summary>
    private void OnKcpDataReceived(ArraySegment<byte> data, kcp2k.KcpChannel channel)
    {
        if (!KcpConnected)
        {
            Logger.Log(LogLevel.Error, "[KCP] Kcp Not Connected!");
            return;
        }

        Logger.Log(LogLevel.Info, $"[KCP] OnKcpDataReceived data.len: {data.Count} channel: {channel}");
        if (data.Array == null)
        {
            Logger.Log(LogLevel.Error, "[KCP] OnKcpDataReceived data.Array == null");
            return;
        }

        var gameManager = GameManager.Instance;
        _kcpClientTransport.OnMessageProcess(data.ToArray(), _memoryStream, cmd =>
        {
            Logger.Log(LogLevel.Info, $"[KCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Count} Channel:{Enum.GetName(typeof(kcp2k.KcpChannel), channel)}");
            switch (cmd)
            {
                case (byte)pb.BattleMsgID.BattleMsgConnect:
                {
                    var s2CMessage = pb.S2C_ConnectMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgConnect -> errorCode:{s2CMessage.ErrorCode}");

                    gameManager.IsBattleConnected = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReady:
                {
                    var s2CMessage = pb.S2C_ReadyMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgReady -> errorCoe:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} status:{s2CMessage.Status}");

                    if (gameManager.IsBattleConnected && gameManager.RoomInfo.RoomId == s2CMessage.RoomId)
                        gameManager.RoomInfo.Readies = s2CMessage.Status.ToList();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgStart:
                {
                    var s2CMessage = pb.S2C_StartMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgStart -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} timestamp:{s2CMessage.TimeStamp}");

                    _serverStopwatch.Start();
                    gameManager.IsBattleStart = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    if (gameManager.IsBattleStart)
                        LoomManager.Instance.QueueOnMainThread(() => gameManager.StartBattle(BattleType.Remote));
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReconnect:
                {
                    var s2CMessage = pb.S2C_BattleReconnectMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgReconnect -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} authoritativeFrame:{s2CMessage.AuthoritativeFrame} playerPos:{s2CMessage.PlayerPos} reason:{s2CMessage.Reason}");

                    if (s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk)
                    {
                        if (gameManager.ReconnectSessionInfo != null)
                            gameManager.ReconnectSessionInfo.PlayerPos = (int)s2CMessage.PlayerPos;
                        LoomManager.Instance.QueueOnMainThread(() => gameManager.HandleBattleReconnectAccepted((int)s2CMessage.AuthoritativeFrame));
                    }
                    else
                    {
                        var reason = string.IsNullOrEmpty(s2CMessage.Reason) ? "重连失败" : s2CMessage.Reason;
                        LoomManager.Instance.QueueOnMainThread(() => gameManager.HandleBattleReconnectFailed(reason));
                    }
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgHeartbeat:
                {
                    var s2CMessage = pb.S2C_HeartbeatMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgHeartbeat -> errorCode:{s2CMessage.ErrorCode} timestamp:{s2CMessage.TimeStamp}");

                    var ping = _serverStopwatch.ElapsedMilliseconds - (long)s2CMessage.TimeStamp;
                    RealPing = ping;
                    MinPing = MinPing == 0 ? ping : Math.Min(MinPing, RealPing);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgHeartbeat -> RealPing:{RealPing} MinPing:{MinPing}");
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgFrame:
                {
                    var s2CMessage = pb.S2C_FrameMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgFrame -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} playerCount:{s2CMessage.PlayerCount} inputCount:{s2CMessage.InputCount} datumCount:{s2CMessage.Datum.Count()}");

                    var inputFrame = FrameBuffer.Frame.defFrame;
                    inputFrame.frame = (int)s2CMessage.Frame;
                    inputFrame.playerCount = (int)s2CMessage.PlayerCount;

                    var byteArray = s2CMessage.Datum.ToByteArray();
                    for (int i = 0; i < inputFrame.playerCount; i++)
                        inputFrame[i] = new FrameBuffer.Input(byte.MaxValue);
                    for (int i = 0; i < BattleSetting.MaxPlayerInRoomCount && i < byteArray.Length; i++)
                    {
                        if ((s2CMessage.InputCount & (1 << i)) == (1 << i))
                            inputFrame[i] = new FrameBuffer.Input(byteArray[i]);
                    }

                    var b0 = byteArray.Length > 0 ? byteArray[0] : (byte)0;
                    var b1 = byteArray.Length > 1 ? byteArray[1] : (byte)0;
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgFrame frame:{inputFrame.frame} B0:[{b0}] B1:[{b1}] D0:[{inputFrame[0]}] D1:[{inputFrame[1]}]");

                    var diff = 0;
                    if (!gameManager.FrameBuffer.SyncFrame(inputFrame.frame, ref inputFrame, ref diff))
                    {
                        Logger.Log(LogLevel.Error, $"[KCP] BattleMsgFrame Can't SyncFrame frame->{inputFrame.frame} diff->{diff}");
                        if (gameManager.IsReconnecting)
                        {
                            LoomManager.Instance.QueueOnMainThread(() => gameManager.HandleBattleReconnectFailed("补帧不同步"));
                        }
                        else
                        {
                            KcpShutdown();
                        }
                        break;
                    }

                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgFrame SyncFrame [frame]->{inputFrame.frame}");
                    gameManager.ServerAuthorityFrame = inputFrame.frame;
                    if (gameManager.IsReconnecting)
                        LoomManager.Instance.QueueOnMainThread(() => gameManager.NotifyReconnectFrameSynced(inputFrame.frame));
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
                case (byte)pb.BattleMsgID.BattleMsgExit:
                {
                    var s2CMessage = pb.S2C_BattleExitMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgExit -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} operatorPlayerId:{s2CMessage.OperatorPlayerId} reason:{s2CMessage.Reason}");

                    if (s2CMessage.ErrorCode != pb.BattleErrorCode.BattleErrBattleOk &&
                        s2CMessage.ErrorCode != pb.BattleErrorCode.BattleErrTimeout)
                    {
                        Logger.Log(LogLevel.Warning, $"[KCP] BattleMsgExit ignored because errorCode:{s2CMessage.ErrorCode}");
                        break;
                    }

                    if (gameManager.RoomInfo == null || gameManager.RoomInfo.RoomId != s2CMessage.RoomId)
                    {
                        Logger.Log(LogLevel.Warning, $"[KCP] BattleMsgExit ignored because room mismatch. local:{gameManager.RoomInfo?.RoomId.ToString() ?? "null"} message:{s2CMessage.RoomId}");
                        break;
                    }

                    var reason = string.IsNullOrEmpty(s2CMessage.Reason) ? "战斗已退出" : s2CMessage.Reason;
                    LoomManager.Instance.QueueOnMainThread(() => gameManager.HandleRemoteBattleExit(reason));
                    break;
                }
            }
        }, _kcpClientTransport.Shutdown);
    }

    /// <summary>
    /// KCP 断开回调
    /// </summary>
    private void OnKcpDisconnected()
    {
        Logger.Log(LogLevel.Info, "OnKcpDisconnected");
        if (GameManager.Instance.IsReconnecting)
            LoomManager.Instance.QueueOnMainThread(() => GameManager.Instance.HandleBattleReconnectFailed("重连连接已断开"));
    }

    /// <summary>
    /// KCP 错误回调
    /// </summary>
    private void OnKcpError(kcp2k.ErrorCode errorCode, string error)
    {
        Logger.Log(LogLevel.Error, $"OnKcpError errorCode: {errorCode} error: {error}");
        if (GameManager.Instance.IsReconnecting)
            LoomManager.Instance.QueueOnMainThread(() => GameManager.Instance.HandleBattleReconnectFailed("重连网络错误"));
    }

    /// <summary>
    /// KCP 连接
    /// </summary>
    public void KcpConnect()
    {
        _kcpClientTransport.Connect(NetSetting.NetAddress);
    }

    /// <summary>
    /// KCP 轮询
    /// </summary>
    public void KcpUpdate()
    {
        _kcpClientTransport.Update();
    }

    /// <summary>
    /// 断开 KCP 连接
    /// </summary>
    public void KcpShutdown()
    {
        Logger.Log(LogLevel.Info, $"[KCP] {KcpUri} Shutdown!");
        _kcpClientTransport.Shutdown();
    }

    /// <summary>
    /// 发送战斗连接消息
    /// </summary>
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
    public void SendBattleReadyMessage(uint roomId, uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_ReadyMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgReady, c2SMessage);
    }

    /// <summary>
    /// 发送战斗重连消息
    /// </summary>
    public void SendBattleReconnectMessage(uint roomId, uint playerId, int lastReceivedFrame)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_BattleReconnectMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        c2SMessage.LastReceivedFrame = 0;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgReconnect, c2SMessage);
    }

    /// <summary>
    /// 发送战斗退出消息
    /// </summary>
    public void SendBattleExitMessage(uint roomId, uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_BattleExitMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgExit, c2SMessage);
    }

    /// <summary>
    /// 发送战斗心跳消息
    /// </summary>
    public void SendBattleHeartBeatMessage(uint playerId, ulong timestamp)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_HeartbeatMsg>();
        c2SMessage.PlayerId = playerId;
        c2SMessage.TimeStamp = timestamp;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgHeartbeat, c2SMessage);
    }

    /// <summary>
    /// 发送战斗帧消息
    /// </summary>
    public void SendBattleFrameMessage(uint frame, byte data)
    {
        _sendFrameByteArray[0] = data;
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_FrameMsg>(true);
        c2SMessage.Frame = frame;
        c2SMessage.Datum = ByteString.CopyFrom(_sendFrameByteArray);
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgFrame, c2SMessage);
    }

    /// <summary>
    /// 发送战斗校验消息
    /// </summary>
    public void SendBattleCheckMessage(int frame, int pos, int md5)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_CheckMsg>();
        c2SMessage.Frame = frame;
        c2SMessage.Pos = pos;
        c2SMessage.Md5 = md5;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgCheck, c2SMessage);
    }

    /// <summary>
    /// 收到 TCP 消息
    /// </summary>
    private void OnTcpDataReceived(byte[] data, int read, NetworkStream stream)
    {
        if (!TcpConnected)
        {
            Logger.Log(LogLevel.Error, "[TCP] Tcp Not Connected!");
            return;
        }

        Logger.Log(LogLevel.Info, $"OnTcpDataReceived data.len: {data.Length} read: {read}");
        var gameManager = GameManager.Instance;
        _tcpClientTransport.OnMessageProcess(data, _memoryStream, cmd =>
        {
            Logger.Log(LogLevel.Info, $"[TCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Length}");
            switch (cmd)
            {
                case (byte)pb.LogicMsgID.LogicMsgLogin:
                {
                    var s2CMessage = pb.S2C_LoginMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[TCP] LogicMsgLogin -> errorCode:{s2CMessage.ErrorCode} playerId:{s2CMessage.PlayerId}");

                    gameManager.PlayerId = s2CMessage.PlayerId;
                    if (s2CMessage.ErrorCode == pb.LogicErrorCode.LogicErrOk && s2CMessage.CanReconnect)
                        LoomManager.Instance.QueueOnMainThread(() => gameManager.BeginReconnectFromLogin(s2CMessage));
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgCreateRoom:
                {
                    var s2CMessage = pb.S2C_CreateRoomMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[TCP] LogicMsgCreateRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId}");

                    if (gameManager.RoomInfo == null)
                        gameManager.RoomInfo = new RoomInfo { RoomId = s2CMessage.RoomId };
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgJoinRoom:
                {
                    var s2CMessage = pb.S2C_JoinRoomMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[TCP] LogicMsgJoinRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} all:{s2CMessage.All}");

                    foreach (var playerId in s2CMessage.All)
                    {
                        if (playerId == gameManager.PlayerId)
                        {
                            gameManager.RoomInfo.Gamers.Clear();
                            gameManager.RoomInfo.Gamers.AddRange(s2CMessage.All);
                        }
                    }

                    if (s2CMessage.All.Count == GameSetting.RoomMaxPlayerCount)
                        LoomManager.Instance.QueueOnMainThread(GameManager.Instance.OnRoomFull);
                    break;
                }
            }
        }, _tcpClientTransport.Shutdown);
    }

    /// <summary>
    /// TCP 连接
    /// </summary>
    public void TcpConnect()
    {
        _tcpClientTransport.Connect(NetSetting.NetAddress);
    }

    /// <summary>
    /// 断开 TCP 连接
    /// </summary>
    public void TcpShutdown()
    {
        Logger.Log(LogLevel.Info, $"[TCP] {TcpUri} Shutdown!");
        _tcpClientTransport.Shutdown();
    }

    /// <summary>
    /// 发送登录消息
    /// </summary>
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
    public void SendLogicCreateRoomMessage(uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_CreateRoomMsg>();
        c2SMessage.PlayerId = playerId;
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgCreateRoom, c2SMessage);
    }

    /// <summary>
    /// 发送加入房间消息
    /// </summary>
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
