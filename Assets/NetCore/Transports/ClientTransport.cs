using System;
using System.IO;
using System.Threading;

/// <summary>
/// 客户端基类
/// </summary>
public abstract class ClientTransport
{
    private int _disconnectRequested;
    /// <summary>
    /// 取消Token
    /// </summary>
    protected CancellationTokenSource TokenSource { get; private set; } = new CancellationTokenSource();
    /// <summary>
    /// 是否已连接
    /// </summary>
    public virtual bool Connected { get; }

    /// <summary>
    /// 地址信息
    /// </summary>
    /// <returns></returns>
    public virtual Uri Uri() { return null; }

    /// <summary>
    /// 连接
    /// </summary>
    /// <param name="address">服务器地址</param>
    public virtual void Connect(string address) { }

    /// <summary>
    /// 复用传输对象重连前，重建已释放的取消句柄。
    /// </summary>
    protected void PrepareForConnect()
    {
        if (Interlocked.Exchange(ref _disconnectRequested, 0) == 0 && !TokenSource.IsCancellationRequested)
            return;

        TokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    protected virtual void Disconnect()
    {
        if (Interlocked.Exchange(ref _disconnectRequested, 1) == 1)
            return;

        try
        {
            TokenSource.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            TokenSource.Dispose();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    /// <summary>
    /// 发送消息包
    /// </summary>
    /// <param name="packet">消息包</param>
    public virtual void Send(Packet packet){}

    /// <summary>
    /// 客户端轮询
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// 断开连接
    /// </summary>
    public virtual void Shutdown() { }

    /// <summary>
    /// 收到服务器消息的处理回调
    /// </summary>
    /// <param name="buffer">字节数据数组</param>
    /// <param name="stream">数据流</param>
    /// <param name="onCommand">处理数据流的回调</param>
    /// <param name="onCatch">发生异常的回调</param>
    /// <param name="onFinally">发生异常的最终处理回调</param>
    public void OnMessageProcess(byte[] buffer, MemoryStream stream, Action<byte> onCommand, Action onCatch = null, Action onFinally = null)
    { 
        var packet = new Packet();
        unsafe
        {
            fixed (byte* src = buffer) packet._head = *((Head*)src);
        }
        try
        {
            stream.Reset();
            stream.Write(buffer, Head.HeadLength, packet._head._length);
            stream.Seek(0, SeekOrigin.Begin);
            onCommand?.Invoke(packet._head._cmd);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[NET] {Uri()} Exception ->\n{ex.Message}\n{ex.StackTrace}");
            onCatch?.Invoke();
        }
        finally
        {
            onFinally?.Invoke();
        }
    }
}
