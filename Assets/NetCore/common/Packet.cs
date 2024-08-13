/// <summary>
/// 消息包
/// </summary>
public struct Packet
{
    /// <summary>
    /// 头数据
    /// </summary>
    public Head _head;
    /// <summary>
    /// 消息本体
    /// </summary>
    public byte[] _data;
}