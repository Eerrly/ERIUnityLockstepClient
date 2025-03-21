using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kcp2k;
using Google.Protobuf;

/// <summary>
/// KCP客户端
/// </summary>
public class KcpClientTransport : ClientTransport
{
    /// <summary>
    /// 端口号
    /// </summary>
    private readonly ushort _port;
    /// <summary>
    /// KCP配置
    /// </summary>
    private readonly KcpConfig _config;
    /// <summary>
    /// KCP客户端
    /// </summary>
    private readonly KcpClient _client;
    /// <summary>
    /// 需要发送的消息包队列
    /// </summary>
    private readonly Queue<Packet> _packets;

    /// <summary>
    /// 已连接回调
    /// </summary>
    public Action OnConnected;
    /// <summary>
    /// 收到消息回调
    /// </summary>
    public Action<ArraySegment<byte>, KcpChannel> OnDataReceived;
    /// <summary>
    /// 断开连接回调
    /// </summary>
    public Action OnDisconnected;
    /// <summary>
    /// 发生错误回调
    /// </summary>
    public Action<ErrorCode, string> OnError;
    /// <summary>
    /// 发生消息后回调
    /// </summary>
    public Action<Packet> OnDataSent;
    
    public KcpClientTransport(KcpConfig config, ushort port)
    {
        _config = config;
        _port = port;
        _client = new KcpClient(
            () => OnConnected?.Invoke(),
            (data, channelId) => OnDataReceived?.Invoke(data, channelId),
            () => OnDisconnected?.Invoke(),
            (errorCode, error) => OnError?.Invoke(errorCode, error),
            _config
        );
        _packets = new Queue<Packet>();
    }

    /// <summary>
    /// 是否已连接
    /// </summary>
    public override bool Connected => _client.connected;

    /// <summary>
    /// 地址信息
    /// </summary>
    /// <returns>地址信息</returns>
    public override Uri Uri()
    {
        var builder = new UriBuilder
        {
            Scheme = nameof(KcpClientTransport),
            Host = System.Net.Dns.GetHostName(),
            Port = _port
        };
        return builder.Uri;
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="address">地址</param>
    public override void Connect(string address)
    {
        _client.Connect(address, _port);
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public override void Disconnect()
    {
        _client.Disconnect();
    }

    /// <summary>
    /// 发生消息包
    /// </summary>
    /// <param name="packet">消息包</param>
    public override void Send(Packet packet)
    {
        try
        {
            _packets.Enqueue(packet);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[KCP] Exception ->\n{ex.Message}\n{ex.StackTrace}");
            Shutdown();
        }
    }

    /// <summary>
    /// 发送消息对象
    /// </summary>
    /// <param name="battleMsgID">消息ID</param>
    /// <param name="message">消息体</param>
    /// <typeparam name="T">消息类型</typeparam>
    public void SendMessage<T>(pb.BattleMsgID battleMsgID, T message) where T : IMessage
    {
        if (!Connected)
        {
            Logger.Log(LogLevel.Error, $"[KCP] Not Connected!");
            return;
        }
        var head = new Head() { _cmd = (byte)battleMsgID, _length = message.CalculateSize() };
        var packet = new Packet() { _data = message.ToByteArray(), _head = head };
        MsgPoolManager.Instance.Release(message);
        Send(packet);
    }

    /// <summary>
    /// KCP客户端轮询
    /// </summary>
    public override void Update() 
    {
        Task.Run(async () => {
            while(true){
                UpdatePacketInfosSent();
                _client.Tick();
                await Task.Delay(TimeSpan.FromMilliseconds(_config.Interval));
            }
        });
    }

    /// <summary>
    /// 处理需要发送的消息队列
    /// </summary>
    private void UpdatePacketInfosSent()
    {
        if(_packets.Count <= 0) return;

        var packet = _packets.Dequeue();
        var buffer = BufferPool.GetBuffer(packet._head._length + Head.HeadLength);
        try
        {
            unsafe
            {
                fixed (byte* src = buffer) *((Head*)src) = packet._head;
            }
            Array.Copy(packet._data, 0, buffer, Head.HeadLength, packet._head._length);
            _client.Send(new ArraySegment<byte>(buffer), KcpChannel.Unreliable);

            Logger.Log(LogLevel.Info,$"[KCP] Send -> MsgID:{Enum.GetName(typeof(pb.BattleMsgID), packet._head._cmd)} dataSize:{packet._head._length}");
            OnDataSent?.Invoke(packet);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[KCP] Exception ->\n{ex.Message}\n{ex.StackTrace}");
            Shutdown();
        }
        finally
        {
            BufferPool.ReleaseBuff(buffer);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public override void Shutdown() => _client.Disconnect();

}