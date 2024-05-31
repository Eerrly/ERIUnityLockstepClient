using System;
using System.Threading;
using System.Threading.Tasks;

public class FrameEngine
{
    private Func<CancellationToken, Task> _frameUpdateListeners = null;
    private Func<CancellationToken, Task> _replayUpdateListeners = null;
    private Func<CancellationToken, Task> _netUpdateListeners = null;

    private Task _frameTask;
    private CancellationTokenSource _frameCancellationTokenSource;

    private Task _replayTask;
    private CancellationTokenSource _replayCancellationTokenSource;

    private Task _netTask;
    private CancellationTokenSource _netCancellationTokenSource;

    public void StartFrameEngine(int interval)
    {
        _frameCancellationTokenSource = new CancellationTokenSource();
        var frameCancellationToken = _frameCancellationTokenSource.Token;
        _frameTask = Task.Run(async () =>
        {
            while (!frameCancellationToken.IsCancellationRequested)
            {
                if (_frameUpdateListeners != null) await _frameUpdateListeners(frameCancellationToken);
                await Task.Delay(interval, frameCancellationToken);
            }
        }, frameCancellationToken);
    }

    private void StopFrameEngine()
    {
        _frameCancellationTokenSource.Cancel();
        _frameTask.Dispose();
    }
    
    public void StartReplayEngine(int interval)
    {
        _replayCancellationTokenSource = new CancellationTokenSource();
        var replayCancellationToken = _replayCancellationTokenSource.Token;
        _replayTask = Task.Run(async () =>
        {
            while (!replayCancellationToken.IsCancellationRequested)
            {
                if (_replayUpdateListeners != null) await _replayUpdateListeners(replayCancellationToken);
                await Task.Delay(interval, replayCancellationToken);
            }
        }, replayCancellationToken);
    }

    public void StopReplayEngine()
    {
        _replayCancellationTokenSource.Cancel();
        _replayTask.Dispose();
    }

    public void StartNetEngine(int interval)
    {
        _netCancellationTokenSource = new CancellationTokenSource();
        var netCancellationToken = _netCancellationTokenSource.Token;
        _netTask = Task.Run(async () =>
        {
            while (!_netCancellationTokenSource.IsCancellationRequested)
            {
                if (_netUpdateListeners != null) await _netUpdateListeners(netCancellationToken);
                await Task.Delay(interval, netCancellationToken);
            }
        }, netCancellationToken);
    }

    private void StopNetEngine()
    {
        _netCancellationTokenSource.Cancel();
    }

    public void StopEngine()
    {
        StopNetEngine();
        StopFrameEngine();
    }
    
    public void RegisterFrameUpdateListener(Func<CancellationToken, Task> listener)
    {
        _frameUpdateListeners = listener;
    }

    public void UnRegisterFrameUpdateListener()
    {
        _frameUpdateListeners = null;
    }
    
    public void RegisterReplayUpdateListener(Func<CancellationToken, Task> listener)
    {
        _replayUpdateListeners = listener;
    }

    public void UnRegisterReplayUpdateListener()
    {
        _replayUpdateListeners = null;
    }

    public void RegisterNetUpdateListener(Func<CancellationToken, Task> listener)
    {
        _netUpdateListeners = listener;
    }

    public void UnRegisterNetUpdateListener()
    {
        _netUpdateListeners -= null;
    }
}