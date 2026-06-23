using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 回放UI根视图
/// </summary>
public class ReplayUIRootView : MonoBehaviour
{
    private static readonly float[] PlaybackSpeeds = { 0.1f, 0.5f, 1f, 2f, 4f };

    public Button PauseBtn;
    public Button PlayBtn;
    public Button SpeedBtn;
    public Button ReturnBtn;
    public Text PauseButtonText;
    public Text PlayButtonText;
    public Text SpeedButtonText;

    private int _speedIndex = 2;
    private bool _listenersBound;
    private GameManager _replayEventSource;

    private void OnEnable()
    {
        ResolveReferences();
        BindListeners();
        _replayEventSource = GameManager.Instance;
        _replayEventSource.OnReplayFinished += HandleReplayFinished;
        SyncSpeedIndex();
        RefreshState();
    }

    private void OnDisable()
    {
        UnbindListeners();
        if (_replayEventSource != null)
        {
            _replayEventSource.OnReplayFinished -= HandleReplayFinished;
            _replayEventSource = null;
        }
    }

    private void BindListeners()
    {
        if (_listenersBound)
            return;

        if (PauseBtn != null) PauseBtn.onClick.AddListener(OnPauseClicked);
        if (PlayBtn != null) PlayBtn.onClick.AddListener(OnPlayClicked);
        if (SpeedBtn != null) SpeedBtn.onClick.AddListener(OnSpeedClicked);
        if (ReturnBtn != null) ReturnBtn.onClick.AddListener(OnReturnClicked);
        _listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!_listenersBound)
            return;

        if (PauseBtn != null) PauseBtn.onClick.RemoveListener(OnPauseClicked);
        if (PlayBtn != null) PlayBtn.onClick.RemoveListener(OnPlayClicked);
        if (SpeedBtn != null) SpeedBtn.onClick.RemoveListener(OnSpeedClicked);
        if (ReturnBtn != null) ReturnBtn.onClick.RemoveListener(OnReturnClicked);
        _listenersBound = false;
    }

    private void OnPauseClicked()
    {
        if (GameManager.Instance.IsReplayFinished)
            return;

        GameManager.Instance.SetReplayPaused(!GameManager.Instance.IsReplayPaused);
        RefreshState();
    }

    private void OnPlayClicked()
    {
        if (GameManager.Instance.IsReplayFinished)
            GameManager.Instance.RestartReplay();
        else
            GameManager.Instance.SetReplayPaused(false);

        RefreshState();
    }

    private void OnSpeedClicked()
    {
        _speedIndex = (_speedIndex + 1) % PlaybackSpeeds.Length;
        GameManager.Instance.SetReplayPlaybackSpeed(PlaybackSpeeds[_speedIndex]);
        RefreshState();
    }

    private void OnReturnClicked()
    {
        GameManager.Instance.ExitReplayToMain();
    }

    private void HandleReplayFinished()
    {
        RefreshState();
    }

    private void RefreshState()
    {
        var gameManager = GameManager.Instance;
        var isFinished = gameManager.IsReplayFinished;
        var isPaused = gameManager.IsReplayPaused;

        if (PauseBtn != null)
            PauseBtn.interactable = !isFinished;

        if (PauseButtonText != null)
            PauseButtonText.text = isPaused ? "继续" : "暂停";

        if (PlayButtonText != null)
            PlayButtonText.text = isFinished ? "重播" : "播放";

        if (SpeedButtonText != null)
            SpeedButtonText.text = $"{gameManager.ReplayPlaybackSpeed:0.##}x";
    }

    private void SyncSpeedIndex()
    {
        var speed = GameManager.Instance.ReplayPlaybackSpeed;
        for (var i = 0; i < PlaybackSpeeds.Length; i++)
        {
            if (Math.Abs(PlaybackSpeeds[i] - speed) < 0.01f)
            {
                _speedIndex = i;
                return;
            }
        }

        _speedIndex = 2;
    }

    private void ResolveReferences()
    {
        PauseBtn ??= FindChildComponent<Button>("PauseBtn");
        PlayBtn ??= FindChildComponent<Button>("PlayBtn");
        SpeedBtn ??= FindChildComponent<Button>("SpeedBtn");
        ReturnBtn ??= FindChildComponent<Button>("ReturnBtn");

        if (PauseButtonText == null && PauseBtn != null)
            PauseButtonText = PauseBtn.GetComponentInChildren<Text>(true);
        if (PlayButtonText == null && PlayBtn != null)
            PlayButtonText = PlayBtn.GetComponentInChildren<Text>(true);
        if (SpeedButtonText == null && SpeedBtn != null)
            SpeedButtonText = SpeedBtn.GetComponentInChildren<Text>(true);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        var children = GetComponentsInChildren<Transform>(true);
        foreach (var child in children)
        {
            if (child.name == childName)
                return child.GetComponent<T>();
        }

        return null;
    }
}
