using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class TcpSupport
{
    private TcpClient _client;
    private byte[] _recvBuffer = new byte[1024];
    private Thread _listenerThread;
    private MemoryStream _memoryStream;
    public bool Connected => _client != null && _client.Connected;

    public TcpSupport()
    {
        _client = new TcpClient();
        _memoryStream = new MemoryStream();
    }

    public void Connect(string address, int port)
    {
        if (_client != null && !_client.Connected)
        {
            _client.BeginConnect(address, port, OnConnectAsync, _client);
        }
    }

    public void Disconnect()
    {
        if (Connected) _client.Close();
        _memoryStream.Dispose();
        _memoryStream = null;
    }

    private void OnConnectAsync(IAsyncResult result)
    {
        try
        {
            var tcpClient = (TcpClient)result.AsyncState;
            tcpClient.EndConnect(result);
            Logger.Log(LogLevel.Info, $"Connected Server Point:{tcpClient.Client.RemoteEndPoint} Start RectThread Listener!");
            StartRecvListener();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private void StartRecvListener()
    {
        _listenerThread = new Thread(new ThreadStart(HandleServerComm));
        _listenerThread.Start();
    }

    private async void HandleServerComm()
    {
        while (true)
        {
            if (!Connected) break;
            var networkStream = _client.GetStream();
            var bytesRead = 0;
            try
            {
                bytesRead = await networkStream.ReadAsync(_recvBuffer, 0, _recvBuffer.Length);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Exception, ex.Message);
            }

            if (bytesRead == 0)
                break;

            OnTcpData(_recvBuffer, bytesRead);
        }
    }

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
    
    public void OnS2CLoginMsg(pb.S2C_LoginMsg s2CLoginMsg)
    {
        Logger.Log(LogLevel.Info,$"[S2C_LoginMsg] ErrorCode:{s2CLoginMsg.ErrorCode} PlayerId:{s2CLoginMsg.PlayerId}");
        if (s2CLoginMsg.ErrorCode == pb.LogicErrorCode.LogicErrOk)
        {
            GameManager.Instance.PlayerId = s2CLoginMsg.PlayerId;
        }
    }

    public void OnS2CCreateRoomMsg(pb.S2C_CreateRoomMsg s2CCreateRoomMsg)
    {
        Logger.Log(LogLevel.Info,$"[S2C_CreateRoomMsg] ErrorCode:{s2CCreateRoomMsg.ErrorCode} RoomId:{s2CCreateRoomMsg.RoomId}");
        if (s2CCreateRoomMsg.ErrorCode == pb.LogicErrorCode.LogicErrOk && !GameManager.Instance.RoomIdList.Contains(s2CCreateRoomMsg.RoomId))
        {
            GameManager.Instance.RoomIdList.Add(s2CCreateRoomMsg.RoomId);
        }
    }

    public void OnS2CJoinRoomMsg(pb.S2C_JoinRoomMsg s2CJoinRoomMsg)
    {
        Logger.Log(LogLevel.Info,$"[S2C_CreateRoomMsg] ErrorCode:{s2CJoinRoomMsg.ErrorCode} RoomId:{s2CJoinRoomMsg.RoomId} All:{s2CJoinRoomMsg.All}");
        if (s2CJoinRoomMsg.ErrorCode == pb.LogicErrorCode.LogicErrOk)
        {
            GameManager.Instance.RoomId = s2CJoinRoomMsg.RoomId;
            GameManager.Instance.RefreshRoomInfo(s2CJoinRoomMsg.RoomId, s2CJoinRoomMsg.All.ToList());
        }
    }
}