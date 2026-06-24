using System;

public class BattleNetworkLifecycleController
{
    private readonly Action _connect;
    private readonly Action _update;
    private readonly Action _shutdown;

    public BattleNetworkLifecycleController()
        : this(
            () => NetworkManager.Instance.KcpConnect(),
            () => NetworkManager.Instance.KcpUpdate(),
            () => NetworkManager.Instance.KcpShutdown())
    {
    }

    public BattleNetworkLifecycleController(Action connect, Action update, Action shutdown)
    {
        _connect = connect ?? throw new ArgumentNullException(nameof(connect));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _shutdown = shutdown ?? throw new ArgumentNullException(nameof(shutdown));
    }

    public void StartBattleConnection()
    {
        _connect();
        _update();
    }

    public void StopBattleConnection()
    {
        _shutdown();
    }
}
