using System;

public class BattleReconnectCompletionFlowActions
{
    public Func<bool> CanComplete;
    public Action BeginCompletion;
    public Func<int> GetTargetFrame;
    public Action<int> SetFrameInterval;
    public Func<int, bool> FastForwardToFrame;
    public Action<Exception> LogFastForwardException;
    public Action<string> FailReconnect;
    public Action EnterRemoteBattleState;
    public Action<int> MarkRuntimeReconnectCompleted;
    public Action<int> AlignServerTimeToFrame;
    public Action<int> MarkReconnectCompleted;
    public Action ApplyBattleCamera;
    public Action InitBattleView;
    public Action ShowBattleRoot;
    public Action StartRemoteBattle;
    public Action FinishCompletion;

    public void Validate()
    {
        if (CanComplete == null)
            throw new ArgumentNullException(nameof(CanComplete));
        if (BeginCompletion == null)
            throw new ArgumentNullException(nameof(BeginCompletion));
        if (GetTargetFrame == null)
            throw new ArgumentNullException(nameof(GetTargetFrame));
        if (SetFrameInterval == null)
            throw new ArgumentNullException(nameof(SetFrameInterval));
        if (FastForwardToFrame == null)
            throw new ArgumentNullException(nameof(FastForwardToFrame));
        if (LogFastForwardException == null)
            throw new ArgumentNullException(nameof(LogFastForwardException));
        if (FailReconnect == null)
            throw new ArgumentNullException(nameof(FailReconnect));
        if (EnterRemoteBattleState == null)
            throw new ArgumentNullException(nameof(EnterRemoteBattleState));
        if (MarkRuntimeReconnectCompleted == null)
            throw new ArgumentNullException(nameof(MarkRuntimeReconnectCompleted));
        if (AlignServerTimeToFrame == null)
            throw new ArgumentNullException(nameof(AlignServerTimeToFrame));
        if (MarkReconnectCompleted == null)
            throw new ArgumentNullException(nameof(MarkReconnectCompleted));
        if (ApplyBattleCamera == null)
            throw new ArgumentNullException(nameof(ApplyBattleCamera));
        if (InitBattleView == null)
            throw new ArgumentNullException(nameof(InitBattleView));
        if (ShowBattleRoot == null)
            throw new ArgumentNullException(nameof(ShowBattleRoot));
        if (StartRemoteBattle == null)
            throw new ArgumentNullException(nameof(StartRemoteBattle));
        if (FinishCompletion == null)
            throw new ArgumentNullException(nameof(FinishCompletion));
    }
}

public class BattleReconnectCompletionFlowController
{
    private const string CatchUpFailedReason = "追帧失败";

    private readonly int _battleInterval;
    private readonly BattleReconnectCompletionFlowActions _actions;

    public BattleReconnectCompletionFlowController(int battleInterval, BattleReconnectCompletionFlowActions actions)
    {
        _battleInterval = battleInterval;
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _actions.Validate();
    }

    public void Complete()
    {
        if (!_actions.CanComplete())
            return;

        _actions.BeginCompletion();
        var targetFrame = _actions.GetTargetFrame();

        try
        {
            _actions.SetFrameInterval(_battleInterval);
            if (!_actions.FastForwardToFrame(targetFrame))
            {
                _actions.FailReconnect(CatchUpFailedReason);
                return;
            }
        }
        catch (Exception ex)
        {
            _actions.LogFastForwardException(ex);
            _actions.FailReconnect(CatchUpFailedReason);
            return;
        }

        _actions.EnterRemoteBattleState();
        _actions.MarkRuntimeReconnectCompleted(targetFrame);
        _actions.AlignServerTimeToFrame(targetFrame);
        _actions.MarkReconnectCompleted(targetFrame);
        _actions.ApplyBattleCamera();
        _actions.InitBattleView();
        _actions.ShowBattleRoot();
        _actions.StartRemoteBattle();
        _actions.FinishCompletion();
    }
}
