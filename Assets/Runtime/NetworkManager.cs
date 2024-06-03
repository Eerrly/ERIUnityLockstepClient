using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using Google.Protobuf;

public class NetworkManager : AManager<NetworkManager>
{
    private Stopwatch _serverStopwatch;
    private KcpClientTransport _kcpClientTransport;
    private TcpClientTransport _tcpClientTransport;
    private MemoryStream _memoryStream;
    private readonly byte[] _sendFrameByteArray = new byte[1];

    public bool KcpConnected => _kcpClientTransport.Connected;
    public bool TcpConnected => _tcpClientTransport.Connected;

    public Uri KcpUri => _kcpClientTransport.Uri();
    public Uri TcpUri => _tcpClientTransport.Uri();

    public long MinPing;
    public long RealPing;


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

    private void OnKcpConnected()
    {
        Logger.Log(LogLevel.Info,$"OnKcpConnected");
    }

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
        _kcpClientTransport.OnMessageProcess(data.ToArray(), _memoryStream, cmd => {
            Logger.Log(LogLevel.Info,$"[KCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Count} Channel:{Enum.GetName(typeof(kcp2k.KcpChannel), channel)}");
            switch (cmd)
            {
                case (byte)pb.BattleMsgID.BattleMsgConnect:
                {
                    var s2CMessage = pb.S2C_ConnectMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgConnect -> errorCode:{s2CMessage.ErrorCode}");

                    GameManager.Instance.IsBattleConnected = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReady:
                {
                    var s2CMessage = pb.S2C_ReadyMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgReady -> errorCoe:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId} status:{s2CMessage.Status}");

                    if(GameManager.Instance.RoomInfo.RoomId == s2CMessage.RoomId)
                        GameManager.Instance.RoomInfo.Readies = s2CMessage.Status.ToList();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgStart:
                {
                    var s2CMessage = pb.S2C_StartMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgStart -> errorCode:{s2CMessage.ErrorCode} frame:{s2CMessage.Frame} timestamp:{s2CMessage.TimeStamp}");

                    _serverStopwatch.Start();
                    GameManager.Instance.IsBattleStart = s2CMessage.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    if (GameManager.Instance.IsBattleStart)
                        GameManager.Instance.StartBattle(BattleType.Remote);
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgHeartbeat:
                {
                    var s2CMessage = pb.S2C_HeartbeatMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgHeartbeat -> errorCode:{s2CMessage.ErrorCode} timestamp:{s2CMessage.TimeStamp}");

                    var ping = _serverStopwatch.ElapsedMilliseconds - (long)s2CMessage.TimeStamp;
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
                    for (int i = 0; i < inputFrame.playerCount; i++)
                        inputFrame[i] = new FrameBuffer.Input(byte.MaxValue);
                    for (int i = 0; i < BattleSetting.MaxPlayerInRoomCount; i++)
                        if ((s2CMessage.InputCount & (1 << i)) == (1 << i)) inputFrame[i] = new FrameBuffer.Input(byteArray[i]);
                    
                    Logger.Log(LogLevel.Info,$"[KCP] BattleMsgFrame frame:{inputFrame.frame} B0:[{byteArray[0]}] B1:[{byteArray[1]}] D0:[{inputFrame[0]}] D1:[{inputFrame[1]}]");

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
                case (byte)pb.BattleMsgID.BattleMsgCheck:
                {
                    var s2CMessage = pb.S2C_CheckMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgCheck -> errorCode:{s2CMessage.ErrorCode}");
                    break;
                }
            }
        }, _kcpClientTransport.Shutdown);
    }

    private void OnKcpDisconnected()
    {
        Logger.Log(LogLevel.Info,$"OnKcpDisconnected");
    }

    private void OnKcpError(kcp2k.ErrorCode errorCode, string error)
    {
        Logger.Log(LogLevel.Error,$"OnKcpError errorCode: {errorCode} error: {error}");
    }

    public void KcpConnect()
    {
        _kcpClientTransport.Connect(NetSetting.NetAddress);
    }

    public void KcpUpdate()
    {
        _kcpClientTransport.Update();
    }

    public void KcpShutdown()
    {
        Logger.Log(LogLevel.Info, $"[KCP] {KcpUri} Shutdown!");
        _kcpClientTransport.Shutdown();
    }

    public void SendBattleConnectMessage(uint playerId, uint seasonId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_ConnectMsg>();
        c2SMessage.PlayerId = playerId;
        c2SMessage.SeasonId = seasonId;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgConnect, c2SMessage);
    }

    public void SendBattleReadyMessage(uint roomId, uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_ReadyMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgReady, c2SMessage);
    }

    public void SendBattleHeartBeatMessage(uint playerId, ulong timestamp)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_HeartbeatMsg>();
        c2SMessage.PlayerId = playerId;
        c2SMessage.TimeStamp = timestamp;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgHeartbeat, c2SMessage);
    }

    public void SendBattleFrameMessage(uint frame, byte data)
    {
        _sendFrameByteArray[0] = data;
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_FrameMsg>();
        c2SMessage.Frame = frame;
        c2SMessage.Datum = ByteString.CopyFrom(_sendFrameByteArray);
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgFrame, c2SMessage);
    }

    public void SendBattleCheckMessage(int frame, int pos, int md5)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_CheckMsg>();
        c2SMessage.Frame = frame;
        c2SMessage.Pos = pos;
        c2SMessage.Md5 = md5;
        _kcpClientTransport.SendMessage(pb.BattleMsgID.BattleMsgCheck, c2SMessage);
    }

    private void OnTcpDataReceived(byte[] data, int read, NetworkStream stream)
    {
        if (!TcpConnected)
        {
            Logger.Log(LogLevel.Error, $"[TCP] Tcp Not Connected!");
            return;
        }
        Logger.Log(LogLevel.Info,$"OnTcpDataReceived data.len: {data.Length} read: {read}");
        _tcpClientTransport.OnMessageProcess(data, _memoryStream, cmd => {
            Logger.Log(LogLevel.Info,$"[TCP] OnMessageProcess -> Cmd:{cmd} Length:{data.Length}");
            switch (cmd)
            {
                case (byte)pb.LogicMsgID.LogicMsgLogin:
                {
                    var s2CMessage = pb.S2C_LoginMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgLogin -> errorCode:{s2CMessage.ErrorCode} playerId:{s2CMessage.PlayerId}");

                    GameManager.Instance.PlayerId = s2CMessage.PlayerId;
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgCreateRoom:
                {
                    var s2CMessage = pb.S2C_CreateRoomMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info,$"[TCP] LogicMsgCreateRoom -> errorCode:{s2CMessage.ErrorCode} roomId:{s2CMessage.RoomId}");
                    
                    if (GameManager.Instance.RoomInfo == null)
                        GameManager.Instance.RoomInfo = new RoomInfo() { RoomId = s2CMessage.RoomId };
                    break;
                }
                case (byte)pb.LogicMsgID.LogicMsgJoinRoom:
                {
                    var s2CMessage = pb.S2C_JoinRoomMsg.Parser.ParseFrom(_memoryStream);
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
        }, _tcpClientTransport.Shutdown);
    }

    public void TcpConnect()
    {
        _tcpClientTransport.Connect(NetSetting.NetAddress);
    }

    public void TcpShutdown()
    {
        Logger.Log(LogLevel.Info, $"[TCP] {TcpUri} Shutdown!");
        _tcpClientTransport.Shutdown();
    }

    public void SendLogicLoginMessage(string account, string password)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_LoginMsg>();
        c2SMessage.Account = ByteString.CopyFromUtf8(account);
        c2SMessage.Password = ByteString.CopyFromUtf8(password);
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgLogin, c2SMessage);
    }

    public void SendLogicCreateRoomMessage(uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_CreateRoomMsg>();
        c2SMessage.PlayerId = playerId;
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgCreateRoom, c2SMessage);
    }

    public void SendLogicJoinRoomMessage(uint roomId, uint playerId)
    {
        var c2SMessage = MsgPoolManager.Instance.Require<pb.C2S_JoinRoomMsg>();
        c2SMessage.RoomId = roomId;
        c2SMessage.PlayerId = playerId;
        _tcpClientTransport.SendMessage(pb.LogicMsgID.LogicMsgJoinRoom, c2SMessage);
    }

}