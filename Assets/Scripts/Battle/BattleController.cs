using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 战斗管理器
/// </summary>
public class BattleController
{
    /// <summary>
    /// 战斗线程任务
    /// </summary>
    private Task _battleTask;
    /// <summary>
    /// 战斗任务取消操作句柄
    /// </summary>
    private CancellationTokenSource _cancellationTokenSource;
    /// <summary>
    /// 客户端与服务器的估算时间差
    /// </summary>
    private long _timeOffset;
    /// <summary>
    /// 最后一次执行Ping的客户端时间
    /// </summary>
    private long _lastHeartbeatTime;
    /// <summary>
    /// 最后一次执行帧数据的客户端时间
    /// </summary>
    private long _lastProcessFrameTime;
    /// <summary>
    /// 客户端计时器
    /// </summary>
    private readonly Stopwatch _stopwatch;
    /// <summary>
    /// 游戏管理对象
    /// </summary>
    private readonly GameManager _gameManager;
    
    public BattleController(GameManager gameManager)
    {
        _stopwatch = new Stopwatch();
        _gameManager = gameManager;
    }

    /// <summary>
    /// 开始客户端计时
    /// </summary>
    public void StartClientStopwatch()
    {
        _stopwatch.Start();
    }

    /// <summary>
    /// 获取客户端时间
    /// </summary>
    /// <returns>客户端流逝的时间</returns>
    public long GetClientStopwatchElapsedMilliseconds()
    {
        return _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// 开启战斗线程任务
    /// </summary>
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
    
    /// <summary>
    /// 战斗线程轮询
    /// </summary>
    /// <param name="cancellationToken">取消操作句柄</param>
    /// <returns>异步任务</returns>
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

    /// <summary>
    /// 取消战斗线程
    /// </summary>
    public void CancelBattleThread()
    {
        _cancellationTokenSource.Cancel();
    }

    /// <summary>
    /// Ping
    /// </summary>
    private void HeartBeat()
    {
        if (_stopwatch.ElapsedMilliseconds - _lastHeartbeatTime < 1000) return;
        
        _lastHeartbeatTime = _stopwatch.ElapsedMilliseconds;
        _gameManager.BattleNetController.SendHeartBeatMsg(_gameManager.PlayerId);
    }

    /// <summary>
    /// 计算客户端与服务器估算的时间差
    /// </summary>
    /// <param name="frame">服务器帧号</param>
    private void CalC2STimeOffset(uint frame)
    {
        if (frame == 0) 
            _timeOffset = 0;
        var timeOffsetShouldBe = _stopwatch.ElapsedMilliseconds - frame * 30;
        if (timeOffsetShouldBe < _timeOffset) 
            _timeOffset = timeOffsetShouldBe;
    }

    /// <summary>
    /// 客户端追帧
    /// </summary>
    private void ClientChaseFrame()
    {
        ConfirmClientFrame();
        _gameManager.BattleNetController.SendFrameMsg(_gameManager.CurClientFrame, _gameManager.Input[_gameManager.CurClientFrame]);
    }

    /// <summary>
    /// 客户端确定帧
    /// </summary>
    private void ConfirmClientFrame()
    {
        ++_gameManager.CurClientFrame;
    }
    
}