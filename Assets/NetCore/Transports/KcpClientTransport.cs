using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using kcp2k;
using Google.Protobuf;

public class KcpClientTransport : ClientTransport
{
    public ushort port {get; private set;}

    public readonly KcpConfig _config;

    public KcpClient _client;

    public Action OnConnected;

    public Action<ArraySegment<byte>, KcpChannel> OnDataReceived;

    public Action OnDisconnected;

    public Action<ErrorCode, string> OnError;

    public Action<Packet> OnDataSent;

    private Queue<Packet> packets;

    public KcpClientTransport(KcpConfig config, ushort port)
    {
        _config = config;
        this.port = port;
        _client = new KcpClient(
            () => OnConnected?.Invoke(),
            (data, channelId) => OnDataReceived?.Invoke(data, channelId),
            () => OnDisconnected?.Invoke(),
            (errorCode, error) => OnError?.Invoke(errorCode, error),
            _config
        );
        packets = new Queue<Packet>();
    }

    public override bool Connected => _client.connected;

    public override Uri Uri()
    {
        var builder = new UriBuilder
        {
            Scheme = nameof(KcpClientTransport),
            Host = System.Net.Dns.GetHostName(),
            Port = port
        };
        return builder.Uri;
    }

    public override void Connect(string address)
    {
        _client.Connect(address, port);
    }

    public override void Disconnect()
    {
        _client.Disconnect();
    }

    public override void Send(Packet packet)
    {
        try
        {
            packets.Enqueue(packet);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[KCP] Exception ->\n{ex.Message}\n{ex.StackTrace}");
            Shutdown();
        }
    }

    public void SendMessage<T>(pb.BattleMsgID battleMsgID, T message) where T : IMessage
    {
        var head = new Head() { _cmd = (byte)battleMsgID, _length = message.CalculateSize() };
        var packet = new Packet() { _data = message.ToByteArray(), _head = head };
        MsgPoolManager.Instance.Release(message);
        Send(packet);
    }

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

    private void UpdatePacketInfosSent()
    {
        if(packets.Count <= 0) return;

        var packet = packets.Dequeue();
        var buffer = BufferPool.GetBuffer(packet._head._length + Head.HeadLength);
        try
        {
            unsafe
            {
                fixed (byte* src = buffer) *((Head*)src) = packet._head;
            }
            Array.Copy(packet._data, 0, buffer, Head.HeadLength, packet._head._length);
            _client.Send(new ArraySegment<byte>(buffer), KcpChannel.Unreliable);

            BufferPool.ReleaseBuff(buffer);
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

    public override void Shutdown() => _client.Disconnect();

}