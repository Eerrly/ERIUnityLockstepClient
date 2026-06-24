using System;

public class BattleEngineLifecycleController
{
    private readonly Action<int> _startNetEngine;
    private readonly Action<int> _startFrameEngine;
    private readonly Action<int> _startReplayEngine;
    private readonly Action _stopEngine;
    private readonly Action _stopReplayEngine;
    private readonly Action<int> _startRecordBattle;
    private readonly Action _releaseBattleRecord;

    public BattleEngineLifecycleController(FrameEngine frameEngine)
        : this(
            interval => frameEngine.StartNetEngine(interval),
            interval => frameEngine.StartFrameEngine(interval),
            interval => frameEngine.StartReplayEngine(interval),
            () => frameEngine.StopEngine(),
            () => frameEngine.StopReplayEngine(),
            playerPosition => BattleRecordManager.Instance.StartRecordBattle(playerPosition),
            () => BattleRecordManager.Instance.OnRelease())
    {
    }

    public BattleEngineLifecycleController(
        Action<int> startNetEngine,
        Action<int> startFrameEngine,
        Action<int> startReplayEngine,
        Action stopEngine,
        Action stopReplayEngine,
        Action<int> startRecordBattle,
        Action releaseBattleRecord)
    {
        _startNetEngine = startNetEngine ?? throw new ArgumentNullException(nameof(startNetEngine));
        _startFrameEngine = startFrameEngine ?? throw new ArgumentNullException(nameof(startFrameEngine));
        _startReplayEngine = startReplayEngine ?? throw new ArgumentNullException(nameof(startReplayEngine));
        _stopEngine = stopEngine ?? throw new ArgumentNullException(nameof(stopEngine));
        _stopReplayEngine = stopReplayEngine ?? throw new ArgumentNullException(nameof(stopReplayEngine));
        _startRecordBattle = startRecordBattle ?? throw new ArgumentNullException(nameof(startRecordBattle));
        _releaseBattleRecord = releaseBattleRecord ?? throw new ArgumentNullException(nameof(releaseBattleRecord));
    }

    public void StartRemoteBattle(int playerPosition)
    {
        _startNetEngine(BattleSetting.NetInterval);
        _startFrameEngine(BattleSetting.BattleInterval);
        _startRecordBattle(playerPosition);
    }

    public void StartReplayBattle()
    {
        _startReplayEngine(BattleSetting.BattleInterval);
    }

    public void StopRemoteBattle()
    {
        _stopEngine();
        _releaseBattleRecord();
    }

    public void StopReplayBattle()
    {
        _stopReplayEngine();
    }
}
