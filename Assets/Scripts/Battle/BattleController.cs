using System.Collections.Generic;
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
    /// 下次发送帧数据时的帧号
    /// </summary>
    private uint _willSentFrame;
    /// <summary>
    /// 客户端计时器
    /// </summary>
    private readonly Stopwatch _stopwatch;
    /// <summary>
    /// 游戏管理对象
    /// </summary>
    private readonly GameManager _gameManager;
    /// <summary>
    /// 预测实体队列
    /// </summary>
    private Queue<Entity> _predictEntityQueue;
    /// <summary>
    /// 服务器返回的操作队列
    /// </summary>
    private Queue<int> _serverDataQueue;
    private Entity _displayEntity;
    /// <summary>
    /// 用于显示的实体对象
    /// </summary>
    public Entity DisplayEntity => _displayEntity;
    private Entity _predictEntity;
    /// <summary>
    /// 预测的实体对象
    /// </summary>
    public Entity PredictEntity => _predictEntity;
    private Entity _confirmEntity;
    /// <summary>
    /// 确定的实体对象
    /// </summary>
    public Entity ConfirmEntity => _confirmEntity;
    /// <summary>
    /// 实体池
    /// </summary>
    private EntityPool _entityPool;
    
    public BattleController(GameManager gameManager)
    {
        _stopwatch = new Stopwatch();
        _gameManager = gameManager;

        _predictEntityQueue = new Queue<Entity>();
        _serverDataQueue = new Queue<int>();
        _confirmEntity = new Entity();
        _predictEntity = new Entity();
        _displayEntity = new Entity();

        _entityPool = new EntityPool();
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
        RollbackConfirm();
        
        var predictClientFrame = _predictEntity.Frame + 1;
        if (predictClientFrame - _gameManager.ServerAuthorityFrame < 4)
        {
            if (_willSentFrame != default)
                _gameManager.BattleNetController.SendFrameMsg(_willSentFrame, _gameManager.Input[predictClientFrame]);

            var maybeServerTime = _stopwatch.ElapsedMilliseconds - _timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = maybeServerTime + NetworkManager.Instance.RealPing * 0.5f >= predictClientFrame * BattleSetting.Interval;
            var slowdown = _stopwatch.ElapsedMilliseconds - _lastProcessFrameTime >= BattleSetting.Interval * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! predictClientFrame:{predictClientFrame} CurServerFrame:{_gameManager.ServerAuthorityFrame}");
            if (hasNewFrame || slowdown)
            {
                _willSentFrame = (uint)predictClientFrame + 1;
                _lastProcessFrameTime = _stopwatch.ElapsedMilliseconds;
                PredictiveExecute();
            }
        }

        HeartBeat();
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// 预测执行
    /// </summary>
    private void PredictiveExecute()
    {
        var newEntity = _entityPool.Dequeue();
        _predictEntity.Frame++;
        _predictEntity.CopyTo(newEntity);
        _predictEntityQueue.Enqueue(newEntity);
        newEntity.CopyTo(_displayEntity);
    }
    
    /// <summary>
    /// 回滚确定
    /// </summary>
    private void RollbackConfirm()
    {
        _serverDataQueue.Clear();
        var pos = _gameManager.GetRoomPos();
        for (var i = 0; i < 4; i++)
        {
            var frame = _confirmEntity.Frame + i + 1;
            if (frame > _gameManager.ServerAuthorityFrame) 
                break;
            _serverDataQueue.Enqueue(_gameManager.FrameInfoDic[frame][pos]);
        }
        
        Logger.Log(LogLevel.Info, $"Rollback _serverDataQueue.Count:{_serverDataQueue.Count} _predictEntityQueue.Count:{_predictEntityQueue.Count} _confirmEntity:{_confirmEntity}");

        var flag = false;
        var confirmPredictEntity = _confirmEntity;
        while (_serverDataQueue.Count > 0)
        {
            var timeOffsetShouldBe = _stopwatch.ElapsedMilliseconds - _gameManager.ServerAuthorityFrame * BattleSetting.Interval;
            if (timeOffsetShouldBe < _timeOffset) 
                _timeOffset = timeOffsetShouldBe;
            
            var lastServerData = _serverDataQueue.Dequeue();
            if (_predictEntityQueue.Count == 0) flag = true;
            if (_predictEntityQueue.Count > 0)
            {
                _predictEntity = _predictEntityQueue.Dequeue();
                if (lastServerData == _predictEntity.Data)
                {
                    _entityPool.Enqueue(confirmPredictEntity);
                    confirmPredictEntity = _predictEntity;
                }
                else
                {
                    flag = true;
                }
            }

            if (!flag) continue;
            
            confirmPredictEntity.Frame++;
            confirmPredictEntity.Data = lastServerData;
        }
        
        Logger.Log(LogLevel.Info, $"Rollback _serverDataQueue.Count:{_serverDataQueue.Count} confirmPredictEntity:{confirmPredictEntity} flag:{flag}");

        if (confirmPredictEntity.Frame != _confirmEntity.Frame || confirmPredictEntity.Data != _confirmEntity.Data)
        {
            confirmPredictEntity.CopyTo(_confirmEntity);
            _entityPool.Enqueue(confirmPredictEntity);
            confirmPredictEntity = _confirmEntity;
        }
        
        Logger.Log(LogLevel.Info, $"Rollback _confirmEntity:{_confirmEntity} _predictEntity:{_predictEntity}");
        
        if (flag)
        {
            confirmPredictEntity.CopyTo(_predictEntity);
            
            var count = _predictEntityQueue.Count;
            for (var i = 0; i < count; i++)
            {
                var entity = _predictEntityQueue.Dequeue();
                entity.Frame++;
                entity.Data = _predictEntity.Data;
                _predictEntityQueue.Enqueue(entity);

                if (i == count - 1) _displayEntity = entity;
            }
        }
        Logger.Log(LogLevel.Info, $"Rollback _predictEntityQueue.Count: {_predictEntityQueue.Count} _predictEntity:{_predictEntity}");
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
        if (_stopwatch.ElapsedMilliseconds - _lastHeartbeatTime < BattleSetting.HeartbeatTime) return;
        
        _lastHeartbeatTime = _stopwatch.ElapsedMilliseconds;
        _gameManager.BattleNetController.SendHeartBeatMsg(_gameManager.PlayerId);
    }
    
}