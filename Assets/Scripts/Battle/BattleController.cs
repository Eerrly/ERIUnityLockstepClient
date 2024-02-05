using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public class BattleController
{
    private Task _battleTask;
    private CancellationTokenSource _cancellationTokenSource;
    private long _timeOffset;
    private long _lastProcessFrameTime;
    
    private readonly Stopwatch _stopwatch;
    private readonly GameManager _gameManager;
    
    public BattleController(GameManager gameManager)
    {
        _stopwatch = new Stopwatch();
        _gameManager = gameManager;
    }

    public void StartClientStopwatch()
    {
        _stopwatch.Start();
    }

    public long GetClientStopwatchElapsedMilliseconds()
    {
        return _stopwatch.ElapsedMilliseconds;
    }

    public void StartBattleThread()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        _battleTask = Task.Run(async () =>
        {
            while (_gameManager.IsBattleStart && !cancellationToken.IsCancellationRequested)
            {
                await ProcessBattle(cancellationToken);
                await Task.Delay(30, cancellationToken);
            }
        }, cancellationToken);
    }
    
    private Task ProcessBattle(CancellationToken cancellationToken)
    {
        if (_gameManager.CurClientFrame < _gameManager.CurServerFrame)
            CalC2STimeOffset(_gameManager.CurServerFrame);

        while (_gameManager.CurClientFrame < _gameManager.CurServerFrame)
            ClientChaseFrame();

        var predictClientFrame = _gameManager.CurClientFrame + 1;
        if (predictClientFrame - _gameManager.CurServerFrame < 4)
        {
            _gameManager.BattleNetController.SendFrameMsg(predictClientFrame, _gameManager.Input[_gameManager.CurClientFrame]);

            var maybeServerTime = _stopwatch.ElapsedMilliseconds - _timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = maybeServerTime + NetworkManager.Instance.RealPing * 0.5f >= predictClientFrame * 30;
            var slowdown = _stopwatch.ElapsedMilliseconds - _lastProcessFrameTime >= 30 * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! CurClientFrame:{_gameManager.CurClientFrame} CurServerFrame:{_gameManager.CurServerFrame}");
            if (hasNewFrame || slowdown)
            {
                _lastProcessFrameTime = _stopwatch.ElapsedMilliseconds;
                ConfirmClientFrame();
            }
        }

        HeartBeat();
        return Task.CompletedTask;
    }

    public void CancelBattleThread()
    {
        _cancellationTokenSource.Cancel();
    }

    private long _lastHeartbeatTime;
    private void HeartBeat()
    {
        if (_stopwatch.ElapsedMilliseconds - _lastHeartbeatTime < 1000) return;
        
        _lastHeartbeatTime = _stopwatch.ElapsedMilliseconds;
        _gameManager.BattleNetController.SendHeartBeatMsg(_gameManager.PlayerId);
    }

    private void CalC2STimeOffset(uint frame)
    {
        if (frame == 0) 
            _timeOffset = 0;
        var timeOffsetShouldBe = _stopwatch.ElapsedMilliseconds - frame * 30;
        if (timeOffsetShouldBe < _timeOffset) 
            _timeOffset = timeOffsetShouldBe;
    }

    private void ClientChaseFrame()
    {
        ConfirmClientFrame();
        _gameManager.BattleNetController.SendFrameMsg(_gameManager.CurClientFrame, _gameManager.Input[_gameManager.CurClientFrame]);
    }

    private void ConfirmClientFrame()
    {
        ++_gameManager.CurClientFrame;
    }
    
}