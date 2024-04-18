using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Google.Protobuf;

public class TcpClientTransport : ClientTransport
{
    public ushort port {get; private set;}
    public Action<byte[], int, NetworkStream> onDataReceived;
    public Action<NetworkStream, Packet> onDataSent;

    private TcpClient _client;
    private int acceptBufferMaxLength;
    private byte[] acceptBuffer;

    public override bool Connected => _client.Connected;

    public TcpClientTransport(ushort port, int acceptBufferMaxLength = 1024)
    {
        this.port = port;
        this.acceptBufferMaxLength = acceptBufferMaxLength;
        this.acceptBuffer = new byte[this.acceptBufferMaxLength];
        _client = new TcpClient();
    }

    public override void Connect(string address)
    {
        _client.BeginConnect(address, port, OnConnectAsync, _client);
    }

    private void OnConnectAsync(IAsyncResult iar)
    {
        var tcpClient = (TcpClient)iar.AsyncState;
        if(tcpClient == null)
        {
            Logger.Log(LogLevel.Error,$"[TCP] OnConnectAsync tcpClient == null");
            return;
        }
        tcpClient.EndConnect(iar);
        Logger.Log(LogLevel.Info,$"[TCP] Connected Server Point: {tcpClient.Client.RemoteEndPoint} Start RecvTask Listener");
        Task.Run(()=> HandleServerCommand());
    }

    private async void HandleServerCommand()
    {
        while (true)
        {
            var stream = _client.GetStream();
            var read = 0;
            try
            {
                read = await stream.ReadAsync(acceptBuffer, 0, acceptBuffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error,$"[TCP] Exception ->\n{ex.Message}\n{ex.StackTrace}");
            }
            if(read == 0) break;
            onDataReceived?.Invoke(acceptBuffer, read, stream);
        }
    }

    public override void Send(Packet packet)
    {
        SendAsync(packet);
    }

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

            BufferPool.ReleaseBuff(buffer);
            Logger.Log(LogLevel.Info,$"[TCP] Send -> MsgID:{Enum.GetName(typeof(pb.LogicMsgID), packet._head._cmd)} dataSize:{packet._head._length}");
            onDataSent?.Invoke(stream, packet);
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

    public void SendMessage(pb.LogicMsgID logicMsgID, IMessage message)
    {
        var head = new Head() { _cmd = (byte)logicMsgID, _length = message.CalculateSize() };
        var packet = new Packet() { _data = message.ToByteArray(), _head = head };
        Send(packet);
    }

    public override void Shutdown()
    {
        _client.Close();
    }

}