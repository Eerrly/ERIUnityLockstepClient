using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Loom : MonoBehaviour
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

        private static Loom _instance;
        public static Loom Instacne
        {
            get
            {
                return _instance;
            }
        }

        private List<FrameItem> _frameDelayed = new List<FrameItem>();

        private static int _maxThreads = 8;
        private static int _threadsCount = 0;

        // main thread -> main thread
        private Queue<Action> _waitingAsyncActions = new Queue<Action>();

        // loom thread -> main thread
        private Queue<Action> _actions = new Queue<Action>();
        private List<DelayItem> _delayed = new List<DelayItem>();

        public static void CreateInstance()
        {
            if (Application.isPlaying && _instance == null)
            {
                var go = new GameObject("Loom");

                go.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                _instance = go.AddComponent<Loom>();
                GameObject.DontDestroyOnLoad(_instance);

                ThreadPool.SetMaxThreads(16, 16);
                ThreadPool.SetMinThreads(8, 8);
            }
        }

        public static void DelayRunAction(Action action)
        {
            DelayRunAction(action, -1);
        }

        public static void DelayRunAction(Action action, float time)
        {
            if (Loom.Instacne)
            {
                var actions = Loom.Instacne._frameDelayed;

                actions.Add(new FrameItem()
                {
                    _frameCount = Time.frameCount,
                    _time = Time.unscaledTime + time,
                    _action = action
                });
            }
        }

        public static void QueueOnMainThread(Action action)
        {
            QueueOnMainThread(action, 0);
        }

        public static void QueueOnMainThread(Action action, float time)
        {
            if (Loom.Instacne)
            {
                if (time > 0f)
                {
                    var delayed = Loom.Instacne._delayed;

                    lock (delayed)
                    {
                        Loom.Instacne._delayed.Add(new Loom.DelayItem
                        {
                            _time = Time.unscaledTime + time,
                            _action = action
                        });
                    }
                }
                else
                {
                    var actions = Loom.Instacne._actions;

                    lock (actions)
                    {
                        actions.Enqueue(action);
                    }
                }
            }
        }

        public static void RunAsync(Action action)
        {
            if (Loom.Instacne)
            {
                if (Loom._threadsCount < Loom._maxThreads)
                {
                    Interlocked.Increment(ref Loom._threadsCount);
                    ThreadPool.QueueUserWorkItem(new WaitCallback(Loom.RunAction), action);
                }
                else
                {
                    Loom.Instacne._waitingAsyncActions.Enqueue(action);
                }
            }
        }

        private static void RunAction(object action)
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
                Interlocked.Decrement(ref Loom._threadsCount);
            }
        }

        public static void Initialize()
        {
            if (Loom.Instacne != null)
            {

            }
        }

        public static void Release()
        {
            if (Loom.Instacne != null)
            {
                Loom.Instacne._actions.Clear();
                Loom.Instacne._delayed.Clear();
            }
        }

        private void Start()
        {
            _instance = this;
        }

        public void OnDestroy()
        {
            _instance = null;
        }

        private void Update()
        {
            if (_waitingAsyncActions.Count > 0 && Loom._threadsCount < Loom._maxThreads)
            {
                Interlocked.Increment(ref Loom._threadsCount);
                ThreadPool.QueueUserWorkItem(new WaitCallback(Loom.RunAction), _waitingAsyncActions.Dequeue());
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