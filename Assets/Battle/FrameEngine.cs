using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 帧引擎
/// </summary>
public class FrameEngine
{
    private Func<CancellationToken, Task> _frameUpdateListeners = null;
    private Func<CancellationToken, Task> _replayUpdateListeners = null;
    private Func<CancellationToken, Task> _netUpdateListeners = null;

    /// <summary>
    /// 战斗线程
    /// </summary>
    private Task _frameTask;
    private CancellationTokenSource _frameCancellationTokenSource;

    /// <summary>
    /// 回放线程
    /// </summary>
    private Task _replayTask;
    private CancellationTokenSource _replayCancellationTokenSource;

    /// <summary>
    /// 网络线程
    /// </summary>
    private Task _netTask;
    private CancellationTokenSource _netCancellationTokenSource;

    private static FixedNumber _frameInterval;
    /// <summary>
    /// 多少秒一帧
    /// </summary>
    public static FixedNumber FrameInterval => _frameInterval;

    /// <summary>
    /// 开启战斗线程
    /// </summary>
    /// <param name="interval">多少秒一轮询</param>
    public void StartFrameEngine(int interval)
    {
        _frameInterval = FixedNumber.MakeFixNum(interval, 1000);
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

    /// <summary>
    /// 关闭战斗线程
    /// </summary>
    private async void StopFrameEngine()
    {
        if(_frameCancellationTokenSource == null || _frameCancellationTokenSource.Token.IsCancellationRequested)
            return;
        _frameCancellationTokenSource.Cancel();
        try
        {
            await _frameTask;
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, $"Exception {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            _frameTask.Dispose();
            _frameCancellationTokenSource.Dispose();
            _frameCancellationTokenSource = null;
        }
    }
    
    /// <summary>
    /// 开启回放线程
    /// </summary>
    /// <param name="interval">多少秒一轮询</param>
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

    /// <summary>
    /// 关闭回放线程
    /// </summary>
    public void StopReplayEngine()
    {
        _replayCancellationTokenSource.Cancel();
        _replayTask.Dispose();
    }

    /// <summary>
    /// 开启网络线程
    /// </summary>
    /// <param name="interval">多少秒一轮询</param>
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

    /// <summary>
    /// 关闭网络线程
    /// </summary>
    private async void StopNetEngine()
    {
        if (_netCancellationTokenSource == null || _netCancellationTokenSource.Token.IsCancellationRequested)
            return;
        _netCancellationTokenSource.Cancel();
        try
        {
            await _netTask;
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, $"Exception {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            _netTask.Dispose();
            _netCancellationTokenSource.Dispose();
            _netCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 关闭战斗相关线程
    /// </summary>
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