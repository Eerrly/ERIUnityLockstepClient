using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 重连加载界面根视图
/// </summary>
public class ReconnectLoadingRootView : MonoBehaviour
{
    public Text TitleText;
    public Text StatusText;
    public Text ProgressText;

    private GameManager _gameManager;

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
            ProgressText.text = string.IsNullOrEmpty(progress) ? string.Empty : progress;
    }

    private string GetStatusText(ReconnectLoadingStatus status)
    {
        switch (status)
        {
            case ReconnectLoadingStatus.Preparing:
                return "准备重连";
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
