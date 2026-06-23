using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗UI根视图
/// </summary>
public class BattleUIRootView : MonoBehaviour
{
    private const float ExitRequestTimeoutSeconds = 5f;

    public Button ExitBattleBtn;
    public Text ExitBattleButtonText;
    public Text StatusText;

    private bool _listenersBound;
    private Coroutine _exitTimeoutCoroutine;

    private void OnEnable()
    {
        ResolveReferences();
        BindListeners();
        SetExitState(false, string.Empty);
    }

    private void OnDisable()
    {
        if (_exitTimeoutCoroutine != null)
        {
            StopCoroutine(_exitTimeoutCoroutine);
            _exitTimeoutCoroutine = null;
        }

        UnbindListeners();
    }

    private void BindListeners()
    {
        if (_listenersBound)
            return;

        if (ExitBattleBtn != null)
            ExitBattleBtn.onClick.AddListener(OnExitBattleClicked);
        _listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!_listenersBound)
            return;

        if (ExitBattleBtn != null)
            ExitBattleBtn.onClick.RemoveListener(OnExitBattleClicked);
        _listenersBound = false;
    }

    private void OnExitBattleClicked()
    {
        var gameManager = GameManager.Instance;
        if (gameManager.RoomInfo == null)
        {
            SetExitState(false, "房间信息不存在，无法退出");
            return;
        }

        NetworkManager.Instance.SendBattleExitMessage(gameManager.RoomInfo.RoomId, gameManager.PlayerId);
        SetExitState(true, "退出中");

        if (_exitTimeoutCoroutine != null)
            StopCoroutine(_exitTimeoutCoroutine);
        _exitTimeoutCoroutine = StartCoroutine(ExitRequestTimeoutRoutine());
    }

    private void SetExitState(bool exiting, string status)
    {
        if (ExitBattleBtn != null)
            ExitBattleBtn.interactable = !exiting;

        if (ExitBattleButtonText != null)
            ExitBattleButtonText.text = exiting ? "退出中" : "退出战斗";

        if (StatusText != null)
            StatusText.text = status;
    }

    private void ResolveReferences()
    {
        ExitBattleBtn ??= FindChildComponent<Button>("ExitBattleBtn");
        StatusText ??= FindChildComponent<Text>("StatusText");
        if (ExitBattleButtonText == null && ExitBattleBtn != null)
            ExitBattleButtonText = ExitBattleBtn.GetComponentInChildren<Text>(true);
    }

    private System.Collections.IEnumerator ExitRequestTimeoutRoutine()
    {
        yield return new WaitForSeconds(ExitRequestTimeoutSeconds);
        _exitTimeoutCoroutine = null;

        Logger.Log(LogLevel.Warning, "Battle exit request timeout, restore exit button.");
        SetExitState(false, "退出请求超时，请重试");
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
