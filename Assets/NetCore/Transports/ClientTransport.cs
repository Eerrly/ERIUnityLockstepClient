using System;
using System.IO;

public abstract class ClientTransport
{
    public virtual bool Connected { get; }

    public virtual Uri Uri() { return null; }

    public virtual void Connect(string address) { }

    public virtual void Disconnect() { }

    public virtual void Send(Packet packet){}

    public virtual void Update() { }

    public virtual void Shutdown() { }

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