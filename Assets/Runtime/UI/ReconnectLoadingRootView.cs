using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 重连加载界面根视图
/// </summary>
public class ReconnectLoadingRootView : MonoBehaviour
{
    private const string DefaultStatusText = "准备重连";
    private const string DefaultProgressText = "等待战斗状态同步";

    public Text TitleText;
    public Text StatusText;
    public Text ProgressText;
    public Image ProgressFillImage;

    private GameManager _gameManager;
    private RectTransform _progressFillRect;

    private void OnEnable()
    {
        ResolveReferences();
        _gameManager = GameManager.Instance;
        if (_gameManager != null)
        {
            _gameManager.OnReconnectLoadingStatusChanged += Refresh;
            Refresh(_gameManager.ReconnectStatus, _gameManager.ReconnectProgressText);
        }
    }

    private void OnDisable()
    {
        if (_gameManager != null)
        {
            _gameManager.OnReconnectLoadingStatusChanged -= Refresh;
            _gameManager = null;
        }
    }

    public void Refresh(ReconnectLoadingStatus status, string progress)
    {
        if (TitleText != null)
            TitleText.text = "断线重连";
        if (StatusText != null)
            StatusText.text = GetStatusText(status);
        if (ProgressText != null)
            ProgressText.text = string.IsNullOrEmpty(progress) ? DefaultProgressText : progress;
        RefreshProgressBar(_gameManager == null ? 0f : _gameManager.ReconnectProgress01);
    }

    private string GetStatusText(ReconnectLoadingStatus status)
    {
        switch (status)
        {
            case ReconnectLoadingStatus.None:
                return DefaultStatusText;
            case ReconnectLoadingStatus.Preparing:
                return DefaultStatusText;
            case ReconnectLoadingStatus.LoadingBattleScene:
                return "加载战斗场景";
            case ReconnectLoadingStatus.ConnectingServer:
                return "连接服务器";
            case ReconnectLoadingStatus.RequestingReconnect:
                return "请求重连";
            case ReconnectLoadingStatus.SyncingBattleProgress:
                return "同步战斗进度";
            case ReconnectLoadingStatus.EnteringBattle:
                return "进入战斗";
            case ReconnectLoadingStatus.Failed:
                return "重连失败";
            default:
                return string.Empty;
        }
    }

    private void ResolveReferences()
    {
        TitleText ??= FindChildComponent<Text>("TitleText");
        StatusText ??= FindChildComponent<Text>("StatusText");
        ProgressText ??= FindChildComponent<Text>("ProgressText");
        ProgressFillImage ??= FindChildComponent<Image>("ProgressGlow");
        _progressFillRect = ProgressFillImage == null ? null : ProgressFillImage.rectTransform;
    }

    private void RefreshProgressBar(float progress01)
    {
        if (_progressFillRect == null)
            return;

        var anchorMax = _progressFillRect.anchorMax;
        anchorMax.x = Mathf.Clamp01(progress01);
        _progressFillRect.anchorMax = anchorMax;
        _progressFillRect.offsetMin = Vector2.zero;
        _progressFillRect.offsetMax = Vector2.zero;
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
