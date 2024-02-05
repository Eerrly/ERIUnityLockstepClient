using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

/// <summary>
/// TCP客户端支持
/// </summary>
public class TcpSupport
{
    /// <summary>
    /// 最大接受消息数组长度
    /// </summary>
    private const int AcceptBufferMaxLength = 1024;
    /// <summary>
    /// TCP客户端对象
    /// </summary>
    private TcpClient _client;
    /// <summary>
    /// 接受数据数组
    /// </summary>
    private byte[] _acceptBuffer;
    /// <summary>
    /// TCP监听线程
    /// </summary>
    private Thread _listenerThread;
    /// <summary>
    /// 数据流
    /// </summary>
    private MemoryStream _memoryStream;
    /// <summary>
    /// 是否连接上服务器
    /// </summary>
    public bool Connected => _client != null && _client.Connected;

    /// <summary>
    /// 服务器返回登录消息委托
    /// </summary>
    public Action<pb.S2C_LoginMsg> OnS2CLoginMsg;
    /// <summary>
    /// 服务器返回创建房间消息委托
    /// </summary>
    public Action<pb.S2C_CreateRoomMsg> OnS2CCreateRoomMsg;
    /// <summary>
    /// 服务器返回加入房间消息委托
    /// </summary>
    public Action<pb.S2C_JoinRoomMsg> OnS2CJoinRoomMsg;

    public TcpSupport()
    {
        _acceptBuffer = new byte[AcceptBufferMaxLength];
        _client = new TcpClient();
        _memoryStream = new MemoryStream();
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="address">IP地址</param>
    /// <param name="port">端口号</param>
    public void Connect(string address, int port)
    {
        if (_client != null && !_client.Connected)
        {
            _client.BeginConnect(address, port, OnConnectAsync, _client);
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (Connected) _client.Close();
        _memoryStream.Dispose();
        _memoryStream = null;
    }

    /// <summary>
    /// 异步连接
    /// </summary>
    /// <param name="result">异步结果</param>
    private void OnConnectAsync(IAsyncResult result)
    {
        try
        {
            var tcpClient = (TcpClient)result.AsyncState;
            tcpClient.EndConnect(result);
            Logger.Log(LogLevel.Info, $"Connected Server Point:{tcpClient.Client.RemoteEndPoint} Start RectThread Listener!");
            StartAcceptListener();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// 开启监听服务器消息
    /// </summary>
    private void StartAcceptListener()
    {
        _listenerThread = new Thread(new ThreadStart(HandleServerComm));
        _listenerThread.Start();
    }

    /// <summary>
    /// 服务器返回消息处理
    /// </summary>
    private async void HandleServerComm()
    {
        while (true)
        {
            if (!Connected) break;
            var networkStream = _client.GetStream();
            var bytesRead = 0;
            try
            {
                bytesRead = await networkStream.ReadAsync(_acceptBuffer, 0, _acceptBuffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Exception, ex.Message);
            }

            if (bytesRead == 0)
                break;

            OnTcpData(_acceptBuffer, bytesRead);
        }
    }

    /// <summary>
    /// 接受服务器返回消息处理
    /// </summary>
    /// <param name="buffer">数据</param>
    /// <param name="bytesRead">已读</param>
    private void OnTcpData(byte[] buffer, int bytesRead)
    {
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
                case (int)pb.LogicMsgID.LogicMsgLogin:
                {
                    OnS2CLoginMsg(pb.S2C_LoginMsg.Parser.ParseFrom(_memoryStream));
                    break;
                }
                case (int)pb.LogicMsgID.LogicMsgCreateRoom:
                {
                    OnS2CCreateRoomMsg(pb.S2C_CreateRoomMsg.Parser.ParseFrom(_memoryStream));
                    break;
                }
                case (int)pb.LogicMsgID.LogicMsgJoinRoom:
                {
                    OnS2CJoinRoomMsg(pb.S2C_JoinRoomMsg.Parser.ParseFrom(_memoryStream));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Exception, ex.Message);
            Disconnect();
        }
    }

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="packet">数据包</param>
    public async void Send(Packet packet)
    {
        if (!Connected) return;

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
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Exception, ex.Message);
            Disconnect();
            _client = null;
        }
        finally
        {
            BufferPool.ReleaseBuff(buffer);
        }
    }
}