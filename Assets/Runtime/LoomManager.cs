using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class LoomManager : MManager<LoomManager>
{
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

    // main thread -> main thread
    private Queue<Action> _waitingAsyncActions = new Queue<Action>();

    // loom thread -> main thread
    private Queue<Action> _actions = new Queue<Action>();
    private List<DelayItem> _delayed = new List<DelayItem>();

    public override void Initialize()
    {
        if (Application.isPlaying)
        {
            ThreadPool.SetMaxThreads(16, 16);
            ThreadPool.SetMinThreads(8, 8);
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
            ThreadPool.QueueUserWorkItem(new WaitCallback(RunAction), action);
        }
        else
        {
            _waitingAsyncActions.Enqueue(action);
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
        if (_waitingAsyncActions.Count > 0 && _threadsCount < _maxThreads)
        {
            Interlocked.Increment(ref _threadsCount);
            ThreadPool.QueueUserWorkItem(new WaitCallback(RunAction), _waitingAsyncActions.Dequeue());
        }

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

                if (delayed._time < Time.unscaledTime)
                {
                    delayed._action.Invoke();
                    _delayed.RemoveAt(i);
                }
            }
        }

        for (var i = _frameDelayed.Count - 1; i >= 0; --i)
        {
            var delayed = _frameDelayed[i];

            if (delayed._frameCount != Time.frameCount)
            {
                if (delayed._time < Time.unscaledTime)
                {
                    delayed._action.Invoke();
                    _frameDelayed.RemoveAt(i);
                }
            }
        }
    }
}