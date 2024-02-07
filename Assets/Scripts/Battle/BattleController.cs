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
    /// 最近一次发送的帧数据
    /// </summary>
    private int _lastSentFrameData;
    /// <summary>
    /// 客户端计时器
    /// </summary>
    private readonly Stopwatch _stopwatch;
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
    
    public BattleController()
    {
        _stopwatch = new Stopwatch();

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
    /// 战斗网络轮询
    /// </summary>
    /// <param name="cancellationToken">取消操作句柄</param>
    /// <returns>异步句柄</returns>
    public Task NetUpdate(CancellationToken cancellationToken)
    {
        var predictClientFrame = _predictEntity.Frame + 1;
        var predictClientFrameData = GameManager.Instance.Input[predictClientFrame];
        if (_willSentFrame != default && predictClientFrameData != default)
        {
            GameManager.Instance.SendFrame(_willSentFrame, predictClientFrameData);
        }

        if (_stopwatch.ElapsedMilliseconds - _lastHeartbeatTime >= BattleSetting.HeartbeatTime)
        {
            _lastHeartbeatTime = _stopwatch.ElapsedMilliseconds;
            GameManager.Instance.Heartbeat();
        }
        
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }
    
    /// <summary>
    /// 战斗线程轮询
    /// </summary>
    /// <param name="cancellationToken">取消操作句柄</param>
    /// <returns>异步任务</returns>
    public Task LogicUpdate(CancellationToken cancellationToken)
    {
        PredictRollback();
        var predictClientFrame = _predictEntity.Frame + 1;
        if (predictClientFrame - GameManager.Instance.ServerAuthorityFrame < 4)
        {
            var maybeServerTime = _stopwatch.ElapsedMilliseconds - _timeOffset + NetworkManager.Instance.MinPing * 0.5f;
            var hasNewFrame = maybeServerTime + NetworkManager.Instance.RealPing * 0.5f >= predictClientFrame * BattleSetting.BattleInterval;
            var slowdown = _stopwatch.ElapsedMilliseconds - _lastProcessFrameTime >= BattleSetting.BattleInterval * 2;
            if (slowdown)
                Logger.Log(LogLevel.Info, $"Slowdown! predictClientFrame:{predictClientFrame} CurServerFrame:{GameManager.Instance.ServerAuthorityFrame}");
            if (hasNewFrame || slowdown)
            {
                _willSentFrame = (uint)predictClientFrame + 1;
                _lastProcessFrameTime = _stopwatch.ElapsedMilliseconds;
                PredictiveExecute();
            }
        }
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }
    
    /// <summary>
    /// 预测执行
    /// </summary>
    private void PredictiveExecute()
    {
        var newEntity = _entityPool.Dequeue();
        // 加入预测实体队列
        _predictEntity.Frame++;
        _predictEntity.CopyTo(newEntity);
        _predictEntityQueue.Enqueue(newEntity);
        
        newEntity.CopyTo(_displayEntity);
    }
    
    /// <summary>
    /// 预测回滚
    /// </summary>
    private void PredictRollback()
    {
        _serverDataQueue.Clear();
        var pos = GameManager.Instance.GetRoomPos();
        // 拿到服务器返回的帧数据到客户端确定帧的帧数据差集合
        for (var i = 0; i < 4; i++)
        {
            var frame = _confirmEntity.Frame + i + 1;
            if (frame > GameManager.Instance.ServerAuthorityFrame) 
                break;
            _serverDataQueue.Enqueue(GameManager.Instance.FrameInfoDic[frame][pos]);
        }
        
        Logger.Log(LogLevel.Info, $"RollbackConfirm _serverDataQueue.Count:{_serverDataQueue.Count} _predictEntityQueue.Count:{_predictEntityQueue.Count} _confirmEntity:{_confirmEntity}");

        var flag = false;
        var confirmPredictEntity = _confirmEntity;
        while (_serverDataQueue.Count > 0)
        {
            // 计算客户端与服务器的估算时间差
            var timeOffsetShouldBe = _stopwatch.ElapsedMilliseconds - GameManager.Instance.ServerAuthorityFrame * BattleSetting.BattleInterval;
            if (timeOffsetShouldBe < _timeOffset) 
                _timeOffset = timeOffsetShouldBe;
            
            var lastServerData = _serverDataQueue.Dequeue();
            if (_predictEntityQueue.Count == 0) flag = true;
            if (_predictEntityQueue.Count > 0)
            {
                //如果帧数据相同，直接使用预测实体
                var tmpPredictEntity = _predictEntityQueue.Dequeue();
                if (lastServerData == tmpPredictEntity.Data)
                {
                    tmpPredictEntity.CopyTo(confirmPredictEntity);
                }
                else
                {
                    flag = true;
                }
                _entityPool.Enqueue(tmpPredictEntity);
            }

            if (!flag) continue;
            
            confirmPredictEntity.Frame++;
            confirmPredictEntity.Data = lastServerData;
        }
        
        Logger.Log(LogLevel.Info, $"RollbackConfirm _serverDataQueue.Count:{_serverDataQueue.Count} confirmPredictEntity:{confirmPredictEntity} flag:{flag}");

        // 如果与确定实体不同，拷贝给确定实体
        if (confirmPredictEntity.Frame != _confirmEntity.Frame || confirmPredictEntity.Data != _confirmEntity.Data)
        {
            confirmPredictEntity.CopyTo(_confirmEntity);
            confirmPredictEntity = _confirmEntity;
        }
        
        Logger.Log(LogLevel.Info, $"RollbackConfirm confirmPredictEntity:{confirmPredictEntity} _predictEntity:{_predictEntity}");
        
        if (flag)
        {
            // 如果预测队列中还存在有预测实体，则按最新的网络帧数据重新赋值
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
        Logger.Log(LogLevel.Info, $"RollbackConfirm _predictEntityQueue.Count: {_predictEntityQueue.Count} _predictEntity:{_predictEntity}");
    }

}