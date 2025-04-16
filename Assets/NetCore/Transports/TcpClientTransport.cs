using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;

/// <summary>
/// TCP客户端
/// </summary>
public class TcpClientTransport : ClientTransport
{
    /// <summary>
    /// 端口号
    /// </summary>
    private readonly ushort _port;
    /// <summary>
    /// TCP客户端
    /// </summary>
    private readonly TcpClient _client;
    /// <summary>
    /// 用于接受字节数据的数组
    /// </summary>
    private readonly byte[] _acceptBuffer;
    
    /// <summary>
    /// 收到消息时回调
    /// </summary>
    public Action<byte[], int, NetworkStream> OnDataReceived;
    /// <summary>
    /// 发送消息后回调
    /// </summary>
    public Action<NetworkStream, Packet> OnDataSent;
    
    /// <summary>
    /// 是否已连接
    /// </summary>
    public override bool Connected => _client.Connected;
    
    public TcpClientTransport(ushort port, int acceptBufferMaxLength = 1024)
    {
        this._port = port;
        this._acceptBuffer = new byte[acceptBufferMaxLength];
        _client = new TcpClient();
    }
    
    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="address">地址</param>
    public override void Connect(string address)
    {
        _client.BeginConnect(address, _port, OnConnectAsync, _client);
    }
    
    /// <summary>
    /// 异步连接服务器
    /// </summary>
    /// <param name="iar"></param>
    private void OnConnectAsync(IAsyncResult iar)
    {
        var tcpClient = (TcpClient)iar.AsyncState;
        if(tcpClient == null)
        {
            Logger.Log(LogLevel.Error,$"[TCP] OnConnectAsync tcpClient == null");
            return;
        }
        tcpClient.EndConnect(iar);
        Logger.Log(LogLevel.Info,$"[TCP] Connected Server Point: {tcpClient.Client.RemoteEndPoint} Start ReceiveTask Listener");
        Task.Run(HandleServerCommand);
    }

    /// <summary>
    /// 处理服务器回复消息
    /// </summary>
    private async void HandleServerCommand()
    {
        while (true)
        {
            var stream = _client.GetStream();
            var read = 0;
            try
            {
                read = await stream.ReadAsync(_acceptBuffer, 0, _acceptBuffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error,$"[TCP] Exception ->\n{ex.Message}\n{ex.StackTrace}");
            }
            if(read == 0) break;
            OnDataReceived?.Invoke(_acceptBuffer, read, stream);
        }
    }
    
    /// <summary>
    /// 发送消息包
    /// </summary>
    /// <param name="packet">消息包</param>
    public override void Send(Packet packet)
    {
        SendAsync(packet);
    }
    
    /// <summary>
    /// 异步发送消息包
    /// </summary>
    /// <param name="packet">消息包</param>
    public async void SendAsync(Packet packet)
    {
        var buffer = BufferPool.GetBuffer(packet._head._length + Head.HeadLength);
        try
        {
            unsafe
            {
                fixed (byte* src = buffer) *((Head*)src) = packet._head;
            }
            Array.Copy(packet._data, 0, buffer, Head.HeadLength, packet._head._length);
            var stream = _client.GetStream();
            await stream.WriteAsync(buffer, 0, buffer.Length);

            Logger.Log(LogLevel.Info,$"[TCP] Send -> MsgID:{Enum.GetName(typeof(pb.LogicMsgID), packet._head._cmd)} dataSize:{packet._head._length}");
            OnDataSent?.Invoke(stream, packet);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[TCP] Exception ->\n{ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            BufferPool.ReleaseBuff(buffer);
        }
    }
    
    /// <summary>
    /// 发送消息对象
    /// </summary>
    /// <param name="logicMsgID">消息ID</param>
    /// <param name="message">消息体</param>
    /// <typeparam name="T">消息类型</typeparam>
    public void SendMessage<T>(pb.LogicMsgID logicMsgID, T message) where T : IMessage
    {
        if (!Connected)
        {
            Logger.Log(LogLevel.Error, $"[TCP] Not Connected!");
            return;
        }
        var head = new Head() { _cmd = (byte)logicMsgID, _length = message.CalculateSize() };
        var packet = new Packet() { _data = message.ToByteArray(), _head = head };
        MsgPoolManager.Instance.Release(message);
        Send(packet);
    }
    
    /// <summary>
    /// 断开连接
    /// </summary>
    public override void Shutdown()
    {
        _client.Close();
    }

}