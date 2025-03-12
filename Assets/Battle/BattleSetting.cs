using UnityEngine;

/// <summary>
/// 战斗常量参数
/// </summary>
public class BattleSetting
{
    /// <summary>
    /// 网络帧间隔(ms)
    /// </summary>
    public const int NetInterval = 1;
    
    /// <summary>
    /// 战斗帧间隔(ms)
    /// </summary>
    public const int BattleInterval = 33;
    
    /// <summary>
    /// 心跳间隔(ms)
    /// </summary>
    public const int HeartbeatTime = 1000;
    
    /// <summary>
    /// 最大房间人数
    /// </summary>
    public const int MaxPlayerInRoomCount = 2;
    
    /// <summary>
    /// 帧缓存数量
    /// </summary>
    public const int MaxFrameCount = 10000;

    /// <summary>
    /// 校验帧间隔
    /// </summary>
    public const int Md5CheckFrame = 200;
    
    /// <summary>
    /// 最大预测帧数据数量
    /// </summary>
    public const int MaxPredictFrameCount = 4;
    
    /// <summary>
    /// 玩家实体预制体
    /// </summary>
    public const string PlayerCharacterPath = "Data/Prefabs/Player";
    
    /// <summary>
    /// 战斗实体预制体
    /// </summary>
    public const string BattleViewPath = "Prefabs/Plane";
    
    /// <summary>
    /// 玩家实体预制体初始化颜色
    /// </summary>
    public static readonly Color[] InitPlayerColor = { new Color((float)42/255, (float)100 /255, (float)178 /255), new Color((float)229/255, (float)46 /255, (float)40 /255) };
}