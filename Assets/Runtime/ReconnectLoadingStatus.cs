/// <summary>
/// 重连加载状态
/// </summary>
public enum ReconnectLoadingStatus
{
    None,
    Preparing,
    LoadingBattleScene,
    ConnectingServer,
    RequestingReconnect,
    SyncingBattleProgress,
    EnteringBattle,
    Failed,
}
