using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Google.Protobuf;

public class NetworkManager : AManager<NetworkManager>
{
    private Stopwatch serverStopwatch;
    private KcpClientTransport kcpClientTransport;
    private TcpClientTransport tcpClientTransport;
    private MemoryStream memoryStream;

    public bool KcpConnected => kcpClientTransport.Connected;
    public bool TcpConnected => tcpClientTransport.Connected;

    public Uri KcpUri => kcpClientTransport.Uri();
    public Uri TcpUri => tcpClientTransport.Uri();

    public long MinPing;
    public long RealPing;


    public override void Initialize()
    {
        serverStopwatch = new Stopwatch();
        memoryStream = new MemoryStream();
        kcpClientTransport = new KcpClientTransport(KcpUtil.defaultConfig, NetSetting.KcpPort);
        kcpClientTransport.onConnected = OnKcpConnected;
        kcpClientTransport.onDataReceived = OnKcpDataReceived;
        kcpClientTransport.onDisconnected = OnkcpDisconnected;
        kcpClientTransport.onError = OnKcpError;
        tcpClientTransport = new TcpClientTransport(NetSetting.TcpPort);
        tcpClientTransport.onDataReceived = OnTcpDataReceived;
    }

    private void OnKcpConnected()
    {
        Logger.Log(LogLevel.Info,$"onConnected");
    }

    private void OnKcpDataReceived(ArraySegment<byte> data, kcp2k.KcpChannel channel)
    {
        Logger.Log(LogLevel.Info,$"OnKcpDataReceived data.len: {data.Count} channel: {channel}");
        if (data.Array == null) {
            Logger.Log(LogLevel.Error,$"kcpClientTransport.onDataReceived data.Array == null");
            return;
        }
        kcpClientTransport.OnMessageProcess(data.ToArray(), memoryStream, cmd => {
            Logger.Log(LogLevel.Info,$"[KCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Count} Channel:{Enum.GetName(typeof(kcp2k.KcpChannel), channel)}");
            switch (cmd)
            {
                case (byte)pb.BattleMsgID.BattleMsgConnect:
                {
                    var s2CMessage = pb.S2C_ConnectMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgConnect -> errorCode:{s2CMessage.ErrorCode}");

                    GameManager.Instance.IsBattleConnected = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReady:
                {
                    var s2CMessage = pb.S2C_ReadyMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgReady -> errorCoe:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} status:{s2CMessage.Status}");

                    if(GameManager.Instance.RoomInfo.RoomId == s2CMessage.RoomId)
                        GameManager.Instance.RoomInfo.Readies = s2CMessage.Status.ToList();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgStart:
                {
                    var s2CMessage = pb.S2C_StartMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgStart -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} timestamp:{s2CMessage.TimeStamp}");

                    serverStopwatch.Start();
                    GameManager.Instance.IsBattleStart = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    if (GameManager.Instance.IsBattleStart)
                        GameManager.Instance.StartBattle();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgHeartbeat:
                {
                    var s2CMessage = pb.S2C_HeartbeatMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgHeartbeat -> errorCode:{s2CMessage.ErrorCode} timestamp:{s2CMessage.TimeStamp}");

                    var ping = serverStopwatch.ElapsedMilliseconds - (long)s2CMessage.TimeStamp;
                    RealPing = ping;
                    MinPing = MinPing == 0 ? ping : Math.Min(MinPing, RealPing);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgHeartbeat -> RealPing:{RealPing} MinPing:{MinPing}");
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgFrame:
                {
                    var s2CMessage = pb.S2C_FrameMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} playerCount:{s2CMessage.PlayerCount} datumCount:{s2CMessage.Datum.Count()}");
                    
                    var inputFrame = FrameBuffer.Frame.defFrame;
                    inputFrame.frame = (int)s2CMessage.Frame;
                    inputFrame.playerCount = (int)s2CMessage.PlayerCount;
                    var byteArray = s2CMessage.Datum.ToByteArray();
                    for (int i = 0; i < inputFrame.playerCount; i++)
                        inputFrame[i] = new FrameBuffer.Input(byteArray[i]);
                    
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame frame:{inputFrame.frame} playerCount:{inputFrame.playerCount} D0:[{inputFrame[0]}] D1:[{inputFrame[1]}]");

                    var diff = 0;
                    while (!GameManager.Instance.FrameBuffer.SyncFrame(inputFrame.frame, ref inputFrame, ref diff))
                    {
                        Logger.Log(LogLevel.Error,$"[KCP] BattleMsgFrame Can't SyncFrame frame->{inputFrame.frame} diff->{diff}");
                        KcpShutdown();
                        break;
                    }

                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame SyncFrame [frame]->{inputFrame.frame}");
                    GameManager.Instance.ServerAuthorityFrame = inputFrame.frame;
                    break;
                }
            }
        }, kcpClientTransport.Shutdown);
    }

    private void OnkcpDisconnected()
    {
        Logger.Log(LogLevel.Info,$"onDisconnected");
    }

    private void OnKcpError(kcp2k.ErrorCode errorCode, string error)
    {
        Logger.Log(LogLevel.Error,$"onError errorCode: {errorCode} error: {error}");
    }

    public void KcpConnect()
    {
        kcpClientTransport.Connect(NetSetting.NetAddress);
    }

    public void KcpUpdate()
    {
        kcpClientTransport.Update();
    }

    public void KcpShutdown()
    {
        kcpClientTransport.Shutdown();
    }

    public void SendBattleConnectMessage(uint playerId, uint seasonId)
    {
        var c2SMessage = new pb.C2S_ConnectMsg(){ PlayerId = playerId, SeasonId = seasonId };
        kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgConnect, c2SMessage);
    }

    public void SendBattleReadyMessage(uint roomId, uint playerId)
    {
        var c2SMessage = new pb.C2S_ReadyMsg() { RoomId = roomId, PlayerId = playerId };
        kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgReady, c2SMessage);
    }

    public void SendBattleHeartBeatMessage(uint playerId, ulong timestamp)
    {
        var c2SMessage = new pb.C2S_HeartbeatMsg(){ PlayerId = playerId, TimeStamp = timestamp };
        kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgHeartbeat, c2SMessage);
    }

    public void SendBattleFrameMessage(uint frame, byte data)
    {
        var byteArray = new byte[] { data };
        var c2SMessage = new pb.C2S_FrameMsg() { Frame = frame, Datum = ByteString.CopyFrom(byteArray) };
        kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgFrame, c2SMessage);
    }

    private void OnTcpDataReceived(byte[] data, int read, NetworkStream stream)
    {
        Logger.Log(LogLevel.Info,$"OnTcpDataReceived data.len: {data.Length} read: {read}");
        tcpClientTransport.OnMessageProcess(data, memoryStream, cmd => {
            Logger.Log(LogLevel.Info,$"[TCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Length}");
            switch (cmd)
            {
                case (byte)pb.LogicMsgID.LogicMsgLogin:
                {
                    var s2CMessage = pb.S2C_LoginMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgLogin -> errorCode:{s2CMessage.ErrorCode} playerId:{s2CMessage.PlayerId}");

                    GameManager.Instance.PlayerId = s2CMessage.PlayerId;
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgCreateRoom:
                {
                    var s2CMessage = pb.S2C_CreateRoomMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgCreateRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId}");
                    
                    if (GameManager.Instance.RoomInfo == null)
                        GameManager.Instance.RoomInfo = new RoomInfo() { RoomId = s2CMessage.RoomId };
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgJoinRoom:
                {
                    var s2CMessage = pb.S2C_JoinRoomMsg.Parser.ParseFrom(memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgJoinRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} all:{s2CMessage.All}");

                    foreach (var playerId in s2CMessage.All)
                    {
                        if (playerId == GameManager.Instance.PlayerId) {
                            GameManager.Instance.RoomInfo.Gamers.Clear();
                            GameManager.Instance.RoomInfo.Gamers.AddRange(s2CMessage.All);
                        }
                    }
                    if (s2CMessage.All.Count == GameSetting.RoomMaxPlayerCount)
                    {
                        KcpConnect();
                        KcpUpdate();
                    }
                    break;
                }
            }
        }, tcpClientTransport.Shutdown);
    }

    public void TcpConnect()
    {
        tcpClientTransport.Connect(NetSetting.NetAddress);
    }

    public void TcpShutdown()
    {
        tcpClientTransport.Shutdown();
    }

    public void SendLogicLoginMessage(string account, string password)
    {
        var c2SMessage = new pb.C2S_LoginMsg(){ Account = ByteString.CopyFromUtf8(account), Password = ByteString.CopyFromUtf8(password) };
        tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgLogin, c2SMessage);
    }

    public void SendLogicCreateRoomMessage(uint playerId)
    {
        var c2SMessage = new pb.C2S_CreateRoomMsg() { PlayerId = playerId };
        tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgCreateRoom, c2SMessage);
    }

    public void SendLogicJoinRoomMessage(uint roomId, uint playerId)
    {
        var c2SMessage = new pb.C2S_JoinRoomMsg() { RoomId = roomId, PlayerId = playerId };
        tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgJoinRoom, c2SMessage);
    }

}