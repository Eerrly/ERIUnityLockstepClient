/// <summary>
/// 战斗运行时状态。Main 映射到 BattleType.Remote 是为了兼容旧 UI 读取 CurrentBattleType 的行为。
/// </summary>
public enum BattleRuntimeState
{
    Main,
    RemoteBattle,
    ReplayBattle,
    ReconnectBattle
}
