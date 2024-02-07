using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using kcp2k;

/// <summary>
/// 网络管理器
/// </summary>
public class NetworkManager : AManager<NetworkManager>
{
    /// <summary>
    /// TCP是否连接
    /// </summary>
    public bool TcpConnected => _tcpSupport != null && _tcpSupport.Connected;
    /// <summary>
    /// KCP是否连接
    /// </summary>
    public bool KcpConnected => _kcpClient!= null && _kcpClient.connected;
    /// <summary>
    /// KCP最小Ping值
    /// </summary>
    public long MinPing;
    /// <summary>
    /// KCP当前Ping值
    /// </summary>
    public long RealPing;
    /// <summary>
    /// KCP轮询多线程异步任务
    /// </summary>
    private Task _kcpTickThread;
    /// <summary>
    /// KCP轮询取消操作句柄
    /// </summary>
    private CancellationTokenSource _kcpTickCancellationTokenSource;
    /// <summary>
    /// KCP配置数据
    /// </summary>
    private KcpConfig _kcpConfig;
    /// <summary>
    /// KCP客户端对象
    /// </summary>
    private KcpClient _kcpClient;
    /// <summary>
    /// 数据流
    /// </summary>
    private MemoryStream _memoryStream;
    /// <summary>
    /// 服务器计时器
    /// </summary>
    private Stopwatch _serverStopwatch;
    /// <summary>
    /// TCP客户端对象
    /// </summary>
    private TcpSupport _tcpSupport;
    /// <summary>
    /// 服务器返回开始消息的服务器帧号
    /// </summary>
    private uint serverStartFrame;
    /// <summary>
    /// 服务器返回开始消息的服务器时间
    /// </summary>
    private ulong serverStartTimestamp;

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        _serverStopwatch = new Stopwatch();
        _memoryStream = new MemoryStream();
        
        _tcpSupport = new TcpSupport();
        _tcpSupport.OnS2CLoginMsg += OnS2CLoginMsg;
        _tcpSupport.OnS2CCreateRoomMsg += OnS2CCreateRoomMsg;
        _tcpSupport.OnS2CJoinRoomMsg += OnS2CJoinRoomMsg;
        
        _kcpTickCancellationTokenSource = new CancellationTokenSource();
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
        _kcpClient = new KcpClient(OnKcpConnected, OnProcessNetKcpAcceptData, OnKcpDisconnected, OnKcpError, _kcpConfig);
    }

    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        _tcpSupport.OnS2CLoginMsg -= OnS2CLoginMsg;
        _tcpSupport.OnS2CCreateRoomMsg -= OnS2CCreateRoomMsg;
        _tcpSupport.OnS2CJoinRoomMsg -= OnS2CJoinRoomMsg;
        
        if(_tcpSupport != null) _tcpSupport.Disconnect();
        if(_kcpTickCancellationTokenSource != null) _kcpTickCancellationTokenSource.Cancel();
        if(_kcpTickThread != null ) _kcpTickThread.Dispose();
        if(_kcpClient != null) _kcpClient.Disconnect();
        if(_memoryStream != null) _memoryStream.Close();
        if(_serverStopwatch != null) _serverStopwatch.Stop();
    }

    /// <summary>
    /// KCP发送数据
    /// </summary>
    /// <param name="packet">数据包</param>
    private void KcpSend(Packet packet)
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
    
    /// <summary>
    /// 发送TCP消息
    /// </summary>
    /// <param name="logicMsgID">消息ID</param>
    /// <param name="msg">消息</param>
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

    /// <summary>
    /// 发送KCP消息
    /// </summary>
    /// <param name="battleMsgID">消息ID</param>
    /// <param name="msg">消息</param>
    public void SendKcpMsg(pb.BattleMsgID battleMsgID, IMessage msg)
    {
        KcpSend(new Packet
        {
            _head = new Head
            {
                _cmd = (byte)battleMsgID,
                _length = msg.CalculateSize()
            },
            _data = msg.ToByteArray()
        });
    }

    /// <summary>
    /// TCP连接服务器
    /// </summary>
    public void TcpConnect()
    {
        _tcpSupport.Connect(NetConstant.NetAddress, NetConstant.TcpPort);
    }

    /// <summary>
    /// KCP连接服务器
    /// </summary>
    public void KcpConnect()
    {
        _kcpClient.Connect(NetConstant.NetAddress, NetConstant.KcpPort);
        var kcpTickCancellationToken = _kcpTickCancellationTokenSource.Token;
        _kcpTickThread = Task.Run(async () =>
        {
            try
            {
                while (_kcpClient != null && !kcpTickCancellationToken.IsCancellationRequested)
                {
                    await KcpTick(kcpTickCancellationToken);
                    await Task.Delay((int)_kcpConfig.Interval, kcpTickCancellationToken);
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Exception, ex.StackTrace);
                _kcpTickCancellationTokenSource.Cancel();
            }
        }, kcpTickCancellationToken);
    }
    
    /// <summary>
    /// KCP轮询
    /// </summary>
    /// <param name="cancellationToken">轮询取消操作句柄</param>
    /// <returns>异步任务</returns>
    private Task KcpTick(CancellationToken cancellationToken)
    {
        _kcpClient.Tick();
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }

    /// <summary>
    /// 连接KCP服务器成功回调
    /// </summary>
    private void OnKcpConnected()
    {
        Logger.Log(LogLevel.Info, $"[KCP] OnClientConnected ");
        var player = GameManager.Instance.GetPlayer();
        SendKcpMsg(pb.BattleMsgID.BattleMsgConnect, new pb.C2S_ConnectMsg()
        {
            PlayerId = player.ID,
            SeasonId = player.RoomId
        });
    }

    /// <summary>
    /// 接受KCP服务器返回数据回调
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="channel">消息类型</param>
    private void OnProcessNetKcpAcceptData(ArraySegment<byte> message, KcpChannel channel)
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
                    GameManager.Instance.IsBattleConnected = s2CMsg.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgReady:
                {
                    var s2CMsg = pb.S2C_ReadyMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgReady ErrorCode:{s2CMsg.ErrorCode} RoomId:{s2CMsg.RoomId} Status:{s2CMsg.Status}");
                    GameManager.Instance.SetRoomStatus(s2CMsg.RoomId, s2CMsg.Status.ToList());
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgStart:
                {
                    var s2CMsg = pb.S2C_StartMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgStart ErrorCode:{s2CMsg.ErrorCode} ServerFrame:{s2CMsg.Frame} ServerTimestamp:{s2CMsg.TimeStamp}");
                    serverStartFrame = s2CMsg.Frame;
                    serverStartTimestamp = s2CMsg.TimeStamp;

                    GameManager.Instance.IsBattleStart = s2CMsg.ErrorCode == pb.BattleErrorCode.BattleErrBattleOk;
                    _serverStopwatch.Start();
                    GameManager.Instance.StartBattle();
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgFrame:
                {
                    var s2CMsg = pb.S2C_FrameMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgFrame ErrorCode:{s2CMsg.ErrorCode} Frame:{s2CMsg.Frame} Datum:{s2CMsg.Datum}");

                    GameManager.Instance.FrameInfoDic[(int)s2CMsg.Frame] = s2CMsg.Datum.ToList();
                    GameManager.Instance.ServerAuthorityFrame = (int)s2CMsg.Frame;
                    break;
                }
                case (byte)pb.BattleMsgID.BattleMsgHeartbeat:
                {
                    var s2CMsg = pb.S2C_HeartbeatMsg.Parser.ParseFrom(_memoryStream);
                    Logger.Log(LogLevel.Info, $"[KCP] BattleMsgHeartbeat ErrorCode:{s2CMsg.ErrorCode} Timestamp:{s2CMsg.TimeStamp}");
                    
                    var ping = _serverStopwatch.ElapsedMilliseconds - (long)s2CMsg.TimeStamp;
                    Instance.RealPing = ping;
                    Instance.MinPing = Instance.MinPing == 0 ? ping : Math.Min(Instance.MinPing, ping);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Exception, ex.Message);
            CloseKcpClient();
        }
    }

    /// <summary>
    /// KCP断开连接回调
    /// </summary>
    private void OnKcpDisconnected()
    {
        Logger.Log(LogLevel.Info, $"[KCP] OnClientDisconnected ");
    }

    /// <summary>
    /// KCP发送错误回调
    /// </summary>
    /// <param name="error">错误码</param>
    /// <param name="reason">错误原因</param>
    private void OnKcpError(ErrorCode error, string reason)
    {
        Logger.Log(LogLevel.Error, $"[KCP] OnServerError({error}, {reason}");
        CloseKcpClient();
    }

    /// <summary>
    /// 关闭KCP客户端
    /// </summary>
    public void CloseKcpClient()
    {
        _kcpTickCancellationTokenSource.Cancel();
        _kcpTickThread.Dispose();
        _kcpClient.Disconnect();
    }
    
    /// <summary>
    /// TCP服务器返回登录消息
    /// </summary>
    /// <param name="s2CLoginMsg">登录消息</param>
    private void OnS2CLoginMsg(pb.S2C_LoginMsg s2CLoginMsg)
    {
        Logger.Log(LogLevel.Info,$"[S2C_LoginMsg] ErrorCode:{s2CLoginMsg.ErrorCode} PlayerId:{s2CLoginMsg.PlayerId}");
        if (s2CLoginMsg.ErrorCode == pb.LogicErrorCode.LogicErrOk)
        {
            GameManager.Instance.InitPlayer(s2CLoginMsg.PlayerId);
        }
    }
    
    /// <summary>
    /// TCP服务器返回创建房间消息
    /// </summary>
    /// <param name="s2CCreateRoomMsg">创建房间消息</param>
    private void OnS2CCreateRoomMsg(pb.S2C_CreateRoomMsg s2CCreateRoomMsg)
    {
        Logger.Log(LogLevel.Info,$"[S2C_CreateRoomMsg] ErrorCode:{s2CCreateRoomMsg.ErrorCode} RoomId:{s2CCreateRoomMsg.RoomId}");
        if (s2CCreateRoomMsg.ErrorCode == pb.LogicErrorCode.LogicErrOk)
        {
            GameManager.Instance.RefreshRoomInfo(s2CCreateRoomMsg.RoomId);
        }
    }
    
    /// <summary>
    /// TCP服务器返回加入房间消息
    /// </summary>
    /// <param name="s2CJoinRoomMsg">加入房间消息</param>
    private void OnS2CJoinRoomMsg(pb.S2C_JoinRoomMsg s2CJoinRoomMsg)
    {
        Logger.Log(LogLevel.Info,$"[S2C_CreateRoomMsg] ErrorCode:{s2CJoinRoomMsg.ErrorCode} RoomId:{s2CJoinRoomMsg.RoomId} All:{s2CJoinRoomMsg.All}");
        if (s2CJoinRoomMsg.ErrorCode == pb.LogicErrorCode.LogicErrOk)
        {
            GameManager.Instance.RefreshRoomInfo(s2CJoinRoomMsg.RoomId, s2CJoinRoomMsg.All.ToList());
        }
    }

    /// <summary>
    /// 关闭TCP客户端
    /// </summary>
    public void CloseTcpClient()
    {
        _tcpSupport.Disconnect();
    }
    
}