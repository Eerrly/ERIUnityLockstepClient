/// <summary>
/// 游戏常量设置
/// </summary>
public class GameSetting
{
    /// <summary>
    /// 渲染帧率
    /// </summary>
    public static readonly int TargetFrameRate = 60;
    
    /// <summary>
    /// 房间最大人数
    /// </summary>
    public static readonly int RoomMaxPlayerCount = 2;

    /// <summary>
    /// 基础玩家ID
    /// </summary>
    public static readonly uint DefaultPlayerIdBase = 100;

    /// <summary>
    /// 基础房间ID
    /// </summary>
    public static readonly uint DefaultRoomIdBase = 10000;
}

/// <summary>
/// 场景
/// </summary>
public enum EGameScene
{
    Main = 0,

    World = 1,

    ReconnectLoading = 2,
}
