using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Protobuf;
using kcp2k;

public class NetworkManager
{
    private static NetworkManager _instance;
    public static NetworkManager Instance => _instance ?? (_instance = new NetworkManager());

    public bool TcpConnected => _tcpSupport != null && _tcpSupport.Connected;
    public bool KcpConnected => _kcpClient!= null && _kcpClient.connected;
    public long MinPing;
    public long RealPing;
    public Stopwatch ServerStopwatch => _stopwatch ?? (_stopwatch = new Stopwatch());

    private Thread _kcpThread;
    private KcpConfig _kcpConfig;
    private KcpClient _kcpClient;
    private MemoryStream _memoryStream;
    private Stopwatch _stopwatch;
    private TcpSupport _tcpSupport;

    public void InitTcpClient()
    {
        _tcpSupport = new TcpSupport();
    }
    
    public void InitKcpClient()
    {
        _memoryStream = new MemoryStream();
        _kcpConfig = new KcpConfig(
            NoDelay: true,
            DualMode: false,
            Interval: 1,
            Timeout: 2000,
            SendWindowSize: Kcp.WND_SND * 1000,
            ReceiveWindowSize: Kcp.WND_RCV * 1000,
            CongestionWindow: false,
            MaxRetransmits: Kcp.DEADLINK * 2
        );
        _kcpClient = new KcpClient(OnKcpConnected, OnKcpData, OnKcpDisconnected, OnKcpError, _kcpConfig);
    }
    
    private void Send(Packet packet)
    {
        if (!KcpConnected) return;

        var buffer = BufferPool.GetBuffer(packet._head._length + Head.HeadLength);
        try
        {
            unsafe
            {
                fixed (byte* src = buffer) *((Head*)src) = packet._head;
            }

            Array.Copy(packet._data, 0, buffer, Head.HeadLength, packet._head._length);
            _kcpClient.Send(new ArraySegment<byte>(buffer), KcpChannel.Unreliable);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Exception, ex.StackTrace);
            _kcpClient.Disconnect();
            _kcpClient = null;
        }
        finally
        {
            BufferPool.ReleaseBuff(buffer);
        }
    }
    
    public void SendTcpMsg(pb.LogicMsgID logicMsgID, IMessage msg)
    {
        _tcpSupport.Send(new Packet
        {
            _head = new Head
            {
                _cmd = (byte)logicMsgID,
                _length = msg.CalculateSize()
            },
            _data = msg.ToByteArray()
        });
    }

    public void SendKcpMsg(pb.BattleMsgID battleMsgID, IMessage msg)
    {
        Send(new Packet
        {
            _head = new Head
            {
                _cmd = (byte)battleMsgID,
                _length = msg.CalculateSize()
            },
            _data = msg.ToByteArray()
        });
    }

    public void TcpConnect()
    {
        _tcpSupport.Connect(NetConstant.NetAddress, NetConstant.TcpPort);
    }

    public void KcpConnect()
    {
        _kcpClient.Connect(NetConstant.NetAddress, NetConstant.KcpPort);
        _kcpThread = new Thread(new ThreadStart(() =>
        {
            while (true)
            {
                _kcpClient.Tick();
                Thread.Sleep((int)_kcpConfig.Interval);
            }
        }));
        _kcpThread.Start();
    }

    private void OnKcpConnected()
    {
        Logger.Log(LogLevel.Info, $"[KCP] OnClientConnected ");
        SendKcpMsg(pb.BattleMsgID.BattleMsgConnect, new pb.C2S_ConnectMsg()
        {
            PlayerId = GameManager.Instance.PlayerId,
            SeasonId = GameManager.Instance.RoomId
        });
    }

    private void OnKcpData(ArraySegment<byte> message, KcpChannel channel)
    {
        var buffer = message.ToArray();
        var packet = new Packet();
        unsafe
        {
            fixed (byte* src = buffer) packet._head = *((Head*)src);
        }

        try
        {
            _memoryStream.Reset();
            _memoryStream.Write(buffer, Head.HeadLength, packet._head._length);
            _memoryStream.Seek(0, SeekOrigin.Begin);

            switch (packet._head._cmd)
            {
                case (byte)pb.BattleMsgID.BattleMsgConnect:
                {
                    var s2CMsg = pb.S2C_ConnectMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgConnect ErrorCode:{s2CMsg.ErrorCode}");
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReady:
                {
                    var s2CMsg = pb.S2C_ReadyMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgReady ErrorCode:{s2CMsg.ErrorCode} Status:{s2CMsg.Status}");
                    GameManager.Instance.Status = s2CMsg.Status.ToList();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgStart:
                {
                    var s2CMsg = pb.S2C_StartMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgStart ErrorCode:{s2CMsg.ErrorCode}");

                    GameManager.Instance.IsBattleStart = true;
                    Instance.ServerStopwatch.Start();
                    GameManager.Instance.StartClientBattleStopwatch();
                    GameManager.Instance.StartClientBattleThread();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgFrame:
                {
                    var s2CMsg = pb.S2C_FrameMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgFrame ErrorCode:{s2CMsg.ErrorCode} Frame:{s2CMsg.Frame} Datum:{s2CMsg.Datum}");

                    GameManager.Instance.FrameInfoDic[s2CMsg.Frame] = s2CMsg.Datum.ToList();
                    GameManager.Instance.CurServerFrame = s2CMsg.Frame;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgHeartbeat:
                {
                    var s2CMsg = pb.S2C_HeartbeatMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgHeartbeat ErrorCode:{s2CMsg.ErrorCode} Timestamp:{s2CMsg.TimeStamp}");
                    
                    var ping = ServerStopwatch.ElapsedMilliseconds - (long)s2CMsg.TimeStamp;
                    Instance.RealPing = ping;
                    Instance.MinPing = Instance.MinPing == 0 ? ping : Math.Min(Instance.MinPing, ping);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Exception, ex.Message);
            _kcpClient.Disconnect();
            _kcpClient = null;
        }
    }

    private void OnKcpDisconnected()
    {
        Logger.Log(LogLevel.Info, $"[KCP] OnClientDisconnected ");
    }

    private void OnKcpError(ErrorCode error, string reason)
    {
        Logger.Log(LogLevel.Error, $"[KCP] OnServerError({error}, {reason}");
    }

    public void CloseKcpClient()
    {
        _kcpClient.Disconnect();
        _kcpThread.Abort();
    }

    public void CloseTcpClient()
    {
        _tcpSupport.Disconnect();
    }
    
}