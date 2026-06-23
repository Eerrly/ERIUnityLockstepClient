using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 线程调度管理器
/// </summary>
public class LoomManager : MManager<LoomManager>
{
    private const int ThreadPoolMaxWorkerThreads = 16;
    private const int ThreadPoolMinWorkerThreads = 8;

    internal struct DelayItem
    {
        public float _time;
        public Action _action;
    }

    internal struct FrameItem
    {
        public int _frameCount;
        public float _time;
        public Action _action;
    }

    private List<FrameItem> _frameDelayed = new List<FrameItem>();

    private int _maxThreads = 8;
    private int _threadsCount = 0;
    private readonly WaitCallback _runActionCallback;

    // main thread -> main thread
    private Queue<Action> _waitingAsyncActions = new Queue<Action>();

    // loom thread -> main thread
    private Queue<Action> _actions = new Queue<Action>();
    private List<DelayItem> _delayed = new List<DelayItem>();

    public LoomManager()
    {
        _runActionCallback = RunAction;
    }

    public override void Initialize()
    {
        if (Application.isPlaying)
        {
            ThreadPool.SetMaxThreads(ThreadPoolMaxWorkerThreads, ThreadPoolMaxWorkerThreads);
            ThreadPool.SetMinThreads(ThreadPoolMinWorkerThreads, ThreadPoolMinWorkerThreads);
        }
    }

    public void DelayRunAction(Action action, float time = -1)
    {
        _frameDelayed.Add(new FrameItem()
        {
            _frameCount = Time.frameCount,
            _time = Time.unscaledTime + time,
            _action = action
        });
    }

    /// <summary>
    /// 加入到主线程调用
    /// </summary>
    /// <param name="action"></param>
    /// <param name="time"></param>
    public void QueueOnMainThread(Action action, float time = 0)
    {
        if (time > 0f)
        {
            lock (_delayed)
            {
                _delayed.Add(new DelayItem
                {
                    _time = Time.unscaledTime + time,
                    _action = action
                });
            }
        }
        else
        {
            lock (_actions)
            {
                _actions.Enqueue(action);
            }
        }
    }

    public void RunAsync(Action action)
    {
        if (_threadsCount < _maxThreads)
        {
            Interlocked.Increment(ref _threadsCount);
            ThreadPool.QueueUserWorkItem(_runActionCallback, action);
        }
        else
        {
            lock (_waitingAsyncActions)
            {
                _waitingAsyncActions.Enqueue(action);
            }
        }
    }

    private void RunAction(object action)
    {
        try
        {
            ((Action)action).Invoke();
        }
        catch (System.Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            Interlocked.Decrement(ref _threadsCount);
        }
    }

    public override void OnRelease()
    {
        _actions.Clear();
        _delayed.Clear();
    }

    private void Update()
    {
        Action waitingAction = null;
        lock (_waitingAsyncActions)
        {
            if (_waitingAsyncActions.Count > 0 && _threadsCount < _maxThreads)
            {
                waitingAction = _waitingAsyncActions.Dequeue();
            }
        }

        if (waitingAction != null)
        {
            Interlocked.Increment(ref _threadsCount);
            ThreadPool.QueueUserWorkItem(_runActionCallback, waitingAction);
        }

        var now = Time.unscaledTime;
        var frameCount = Time.frameCount;

        lock (_actions)
        {
            while (_actions.Count > 0)
            {
                _actions.Dequeue().Invoke();
            }
        }

        lock (_delayed)
        {
            for (var i = _delayed.Count - 1; i >= 0; --i)
            {
                var delayed = _delayed[i];

                if (delayed._time < now)
                {
                    delayed._action.Invoke();
                    _delayed.RemoveAt(i);
                }
            }
        }

        for (var i = _frameDelayed.Count - 1; i >= 0; --i)
        {
            var delayed = _frameDelayed[i];

            if (delayed._frameCount != frameCount)
            {
                if (delayed._time < now)
                {
                    delayed._action.Invoke();
                    _frameDelayed.RemoveAt(i);
                }
            }
        }
    }
}
