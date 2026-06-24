using System;

public class BattleRuntimeFlagController
{
    private readonly Action<bool> _setBattleConnected;
    private readonly Action<bool> _setBattleStarted;
    private readonly Action<int> _setServerAuthorityFrame;

    public BattleRuntimeFlagController(
        Action<bool> setBattleConnected,
        Action<bool> setBattleStarted,
        Action<int> setServerAuthorityFrame)
    {
        _setBattleConnected = setBattleConnected ?? throw new ArgumentNullException(nameof(setBattleConnected));
        _setBattleStarted = setBattleStarted ?? throw new ArgumentNullException(nameof(setBattleStarted));
        _setServerAuthorityFrame = setServerAuthorityFrame ?? throw new ArgumentNullException(nameof(setServerAuthorityFrame));
    }

    public void SetBattleConnected(bool isConnected)
    {
        _setBattleConnected(isConnected);
    }

    public void SetBattleStarted(bool isStarted)
    {
        _setBattleStarted(isStarted);
    }

    public void SetServerAuthorityFrame(int frame)
    {
        _setServerAuthorityFrame(frame);
    }

    public void MarkBattleStarted()
    {
        _setBattleStarted(true);
    }

    public void MarkBattleStopped()
    {
        _setBattleStarted(false);
    }

    public void MarkRemoteBattleStopped()
    {
        _setBattleConnected(false);
        _setBattleStarted(false);
    }

    public void ResetBattleRuntime()
    {
        _setBattleConnected(false);
        _setBattleStarted(false);
        _setServerAuthorityFrame(-1);
    }

    public void MarkReconnectCompleted(int authoritativeFrame)
    {
        _setBattleStarted(true);
        _setBattleConnected(true);
        _setServerAuthorityFrame(authoritativeFrame);
    }
}
