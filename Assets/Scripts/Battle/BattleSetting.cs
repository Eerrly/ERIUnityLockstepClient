public class BattleSetting
{
    /// <summary>
    /// 网络轮询间隔
    /// </summary>
    public const int NetInterval = 1;
    
    /// <summary>
    /// 战斗逻辑轮询间隔
    /// </summary>
    public const int BattleInterval = 30;

    /// <summary>
    /// Ping消息间隔
    /// </summary>
    public const int HeartbeatTime = 1000;

    /// <summary>
    /// 房间最大人数
    /// </summary>
    public const int MaxPlayerInRoomCount = 2;

    /// <summary>
    /// 最大帧缓存
    /// </summary>
    public const int MaxFrameCount = 1000;

    /// <summary>
    /// 最大预测缓存队列长度
    /// </summary>
    public const int MaxPredictFrameCount = 4;
}