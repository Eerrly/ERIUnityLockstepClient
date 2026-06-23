using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 回放控制器
/// </summary>
public class ReplayController
{
    public event Action OnReplayFinished;

    private readonly object _playbackLock = new object();

    /// <summary>
    /// 下一个待播放帧
    /// </summary>
    private int _frame;
    private bool _isPaused;
    private bool _isReplayFinished;
    private bool _pendingFinishNotification;
    private float _playbackSpeed;
    private long _playbackElapsedMilliseconds;
    private long _lastStopwatchMilliseconds;
    private readonly Stopwatch _stopwatch;

    /// <summary>
    /// 缓存战斗实体列表
    /// </summary>
    private readonly List<BattleEntity> _battleEntities;

    private readonly BattleEntity _displayBattleEntity;

    /// <summary>
    /// 渲染实体
    /// </summary>
    public BattleEntity DisplayBattleEntity => _displayBattleEntity;

    public bool IsPaused
    {
        get
        {
            lock (_playbackLock)
                return _isPaused;
        }
    }

    public float PlaybackSpeed
    {
        get
        {
            lock (_playbackLock)
                return _playbackSpeed;
        }
    }

    public bool IsReplayFinished
    {
        get
        {
            lock (_playbackLock)
                return _isReplayFinished;
        }
    }

    public ReplayController()
    {
        _frame = 0;
        _isPaused = false;
        _isReplayFinished = false;
        _pendingFinishNotification = false;
        _playbackSpeed = 1f;
        _stopwatch = new Stopwatch();
        _battleEntities = new List<BattleEntity>();
        _displayBattleEntity = new BattleEntity();
        _displayBattleEntity.PlayerEntities.Add(new PlayerEntity(){ ID = 0 });
        _displayBattleEntity.PlayerEntities.Add(new PlayerEntity(){ ID = 1 });
    }

    /// <summary>
    /// 开始客户端计时器
    /// </summary>
    public void StartReplayStopwatch()
    {
        lock (_playbackLock)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            _lastStopwatchMilliseconds = 0;
        }
    }

    /// <summary>
    /// 初始化回放数据
    /// </summary>
    /// <param name="pos">战斗POS</param>
    public void InitReplay(int pos)
    {
        lock (_playbackLock)
        {
            _frame = 0;
            _isPaused = false;
            _isReplayFinished = false;
            _pendingFinishNotification = false;
            _playbackElapsedMilliseconds = 0;
            _lastStopwatchMilliseconds = 0;
            _battleEntities.Clear();
            _stopwatch.Reset();
        }

        // 最近的一次多人战斗记录
        var battleRecordPath = $"{Application.persistentDataPath}/battle_record_{pos}.log";
        if (!File.Exists(battleRecordPath))
        {
            Logger.Log(LogLevel.Error, $"InitReplay file not found: {battleRecordPath}");
            MarkReplayFinishedPending();
            return;
        }

        try
        {
            using var fs = new FileStream(battleRecordPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);
            while (fs.Position < fs.Length)
            {
                var entity = new BattleEntity { BattleEntityType = EBattleEntityType.Display };
                entity.Deserialize(br);
                lock (_playbackLock)
                    _battleEntities.Add(entity);
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"InitReplay Deserialize Failed: {ex.Message}\n{ex.StackTrace}");
            MarkReplayFinishedPending();
            return;
        }

        lock (_playbackLock)
        {
            if (_battleEntities.Count <= 0)
            {
                Logger.Log(LogLevel.Warning, $"InitReplay empty replay file: {battleRecordPath}");
                MarkReplayFinishedPendingLocked();
            }
        }
    }

    public void SetPaused(bool isPaused)
    {
        lock (_playbackLock)
        {
            if (_isReplayFinished && !isPaused)
                return;

            SyncPlaybackElapsedLocked();
            _isPaused = isPaused;
            _lastStopwatchMilliseconds = _stopwatch.ElapsedMilliseconds;
        }
    }

    public void SetPlaybackSpeed(float playbackSpeed)
    {
        lock (_playbackLock)
        {
            SyncPlaybackElapsedLocked();
            _playbackSpeed = Mathf.Clamp(playbackSpeed, 0.1f, 4f);
            _lastStopwatchMilliseconds = _stopwatch.ElapsedMilliseconds;
        }
    }

    public void RestartReplay()
    {
        lock (_playbackLock)
        {
            _frame = 0;
            _isPaused = false;
            _isReplayFinished = false;
            _pendingFinishNotification = false;
            _playbackElapsedMilliseconds = 0;
            _lastStopwatchMilliseconds = 0;

            if (_battleEntities.Count > 0)
            {
                _battleEntities[0].CopyTo(_displayBattleEntity);
                _frame = 1;
            }
            else
            {
                MarkReplayFinishedPendingLocked();
            }

            _stopwatch.Reset();
            _stopwatch.Start();
        }
    }

    /// <summary>
    /// 渲染回放
    /// </summary>
    /// <param name="cancellationToken">任务取消句柄</param>
    /// <returns>任务</returns>
    public Task ReplayUpdate(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        var shouldNotifyFinished = false;
        lock (_playbackLock)
        {
            if (_pendingFinishNotification)
            {
                _pendingFinishNotification = false;
                shouldNotifyFinished = true;
            }

            if (!_isReplayFinished && !_isPaused && _battleEntities.Count > 0)
            {
                SyncPlaybackElapsedLocked();

                while (_frame < _battleEntities.Count &&
                       _playbackElapsedMilliseconds >= (long)_frame * BattleSetting.BattleInterval)
                {
                    _battleEntities[_frame].CopyTo(_displayBattleEntity);
                    _frame++;
                }

                if (_frame >= _battleEntities.Count)
                {
                    _isReplayFinished = true;
                    _isPaused = true;
                    shouldNotifyFinished = true;
                }
            }
        }

        if (shouldNotifyFinished)
            OnReplayFinished?.Invoke();

        return Task.CompletedTask;
    }

    private void MarkReplayFinishedPending()
    {
        lock (_playbackLock)
        {
            MarkReplayFinishedPendingLocked();
        }
    }

    private void MarkReplayFinishedPendingLocked()
    {
        _isReplayFinished = true;
        _isPaused = true;
        _pendingFinishNotification = true;
    }

    private void SyncPlaybackElapsedLocked()
    {
        if (_isPaused || _isReplayFinished)
            return;

        if (!_stopwatch.IsRunning)
            _stopwatch.Start();

        var elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
        var deltaMilliseconds = Math.Max(0, elapsedMilliseconds - _lastStopwatchMilliseconds);
        _playbackElapsedMilliseconds += (long)(deltaMilliseconds * _playbackSpeed);
        _lastStopwatchMilliseconds = elapsedMilliseconds;
    }
}
