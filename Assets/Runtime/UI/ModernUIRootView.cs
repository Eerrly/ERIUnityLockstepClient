using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主界面UI根视图
/// </summary>
public class ModernUIRootView : MonoBehaviour
{
    public InputField AccountInputField;
    public InputField PasswordInputField;
    public Button AttachBtn;
    public Button LoginBtn;
    public Button CreateRoomBtn;
    public Button JoinRoomBtn;
    public Button ConnectBtn;
    public Button ReadyBtn;
    public Button ShutdownBtn;
    public Button ReplayBtn;
    public Dropdown SelectReplayDropdown;
    public Text ToastText;

    private Coroutine _toastCoroutine;
    private bool _listenersBound;
    private GameManager _statusMessageSource;

    private void OnEnable()
    {
        ResolveReferences();
        BindListeners();
        UpdateReplayDropDownInfo();
        _statusMessageSource = GameManager.Instance;
        _statusMessageSource.OnStatusMessage += ShowToast;
    }

    private void OnDisable()
    {
        UnbindListeners();
        if (_statusMessageSource != null)
        {
            _statusMessageSource.OnStatusMessage -= ShowToast;
            _statusMessageSource = null;
        }
    }

    public void ShowToast(string message)
    {
        EnsureToast();
        if (_toastCoroutine != null)
            StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(ShowToastRoutine(message));
    }

    private void BindListeners()
    {
        if (_listenersBound)
            return;

        if (AttachBtn != null) AttachBtn.onClick.AddListener(OnAttachClicked);
        if (LoginBtn != null) LoginBtn.onClick.AddListener(OnLoginClicked);
        if (CreateRoomBtn != null) CreateRoomBtn.onClick.AddListener(OnCreateRoomClicked);
        if (JoinRoomBtn != null) JoinRoomBtn.onClick.AddListener(OnJoinRoomClicked);
        if (ConnectBtn != null) ConnectBtn.onClick.AddListener(OnConnectClicked);
        if (ReadyBtn != null) ReadyBtn.onClick.AddListener(OnReadyClicked);
        if (ShutdownBtn != null) ShutdownBtn.onClick.AddListener(OnShutdownClicked);
        if (ReplayBtn != null) ReplayBtn.onClick.AddListener(OnReplayClicked);
        _listenersBound = true;
    }

    private void UnbindListeners()
    {
        if (!_listenersBound)
            return;

        if (AttachBtn != null) AttachBtn.onClick.RemoveListener(OnAttachClicked);
        if (LoginBtn != null) LoginBtn.onClick.RemoveListener(OnLoginClicked);
        if (CreateRoomBtn != null) CreateRoomBtn.onClick.RemoveListener(OnCreateRoomClicked);
        if (JoinRoomBtn != null) JoinRoomBtn.onClick.RemoveListener(OnJoinRoomClicked);
        if (ConnectBtn != null) ConnectBtn.onClick.RemoveListener(OnConnectClicked);
        if (ReadyBtn != null) ReadyBtn.onClick.RemoveListener(OnReadyClicked);
        if (ShutdownBtn != null) ShutdownBtn.onClick.RemoveListener(OnShutdownClicked);
        if (ReplayBtn != null) ReplayBtn.onClick.RemoveListener(OnReplayClicked);
        _listenersBound = false;
    }

    private void OnAttachClicked()
    {
        NetworkManager.Instance.TcpConnect();
    }

    private void OnLoginClicked()
    {
        NetworkManager.Instance.SendLogicLoginMessage(
            AccountInputField == null ? string.Empty : AccountInputField.text,
            PasswordInputField == null ? string.Empty : PasswordInputField.text);
    }

    private void OnCreateRoomClicked()
    {
        NetworkManager.Instance.SendLogicCreateRoomMessage(GameManager.Instance.PlayerId);
    }

    private void OnJoinRoomClicked()
    {
        if (GameManager.Instance.RoomInfo == null)
        {
            ShowToast("请先创建房间");
            return;
        }

        NetworkManager.Instance.SendLogicJoinRoomMessage(GameManager.Instance.RoomInfo.RoomId, GameManager.Instance.PlayerId);
    }

    private void OnConnectClicked()
    {
        NetworkManager.Instance.SendBattleConnectMessage(GameManager.Instance.PlayerId, 2);
    }

    private void OnReadyClicked()
    {
        if (GameManager.Instance.RoomInfo == null)
        {
            ShowToast("请先进入房间");
            return;
        }

        NetworkManager.Instance.SendBattleReadyMessage(GameManager.Instance.RoomInfo.RoomId, GameManager.Instance.PlayerId);
    }

    private void OnShutdownClicked()
    {
        GameManager.Instance.StopBattle(GameManager.Instance.CurrentBattleType);
        CameraManager.Instance.ApplyMainCameraState();
    }

    private void OnReplayClicked()
    {
        if (SelectReplayDropdown == null || SelectReplayDropdown.options.Count <= 0 || !ReplayBtn.interactable)
            return;

        var selectedText = SelectReplayDropdown.options[SelectReplayDropdown.value].text;
        var matches = Regex.Matches(selectedText, @"battle_record_(\d+).log");
        if (matches.Count <= 0)
            return;

        if (!uint.TryParse(matches[0].Groups[1].Value, out var pos))
            return;

        GameManager.Instance.PlayerId = pos + GameSetting.DefaultPlayerIdBase + 1;
        GameManager.Instance.StartBattle(BattleType.Replay);
    }

    private void UpdateReplayDropDownInfo()
    {
        if (SelectReplayDropdown == null || ReplayBtn == null)
            return;

        SelectReplayDropdown.options.Clear();
        var replayPaths = Directory.GetFiles(Application.persistentDataPath, "battle_record_*.log");
        foreach (var path in replayPaths)
        {
            SelectReplayDropdown.options.Add(new Dropdown.OptionData
            {
                text = Path.GetFileName(path)
            });
        }

        if (SelectReplayDropdown.options.Count > 0)
        {
            SelectReplayDropdown.value = 0;
            SelectReplayDropdown.RefreshShownValue();
            ReplayBtn.interactable = true;
        }
        else
        {
            SelectReplayDropdown.options.Add(new Dropdown.OptionData
            {
                text = "无回放记录"
            });
            SelectReplayDropdown.value = 0;
            SelectReplayDropdown.RefreshShownValue();
            ReplayBtn.interactable = false;
        }
    }

    private void EnsureToast()
    {
        if (ToastText != null)
            return;

        var toastRoot = new GameObject("ReplayToast", typeof(RectTransform), typeof(Image));
        toastRoot.layer = gameObject.layer;
        toastRoot.transform.SetParent(transform, false);

        var rect = toastRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(320f, 56f);
        rect.anchoredPosition = new Vector2(0f, -24f);

        var image = toastRoot.GetComponent<Image>();
        image.color = new Color(0.1f, 0.12f, 0.18f, 0.92f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.layer = gameObject.layer;
        textGo.transform.SetParent(toastRoot.transform, false);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 8f);
        textRect.offsetMax = new Vector2(-10f, -8f);

        ToastText = textGo.GetComponent<Text>();
        ToastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        ToastText.fontSize = 24;
        ToastText.color = Color.white;
        ToastText.alignment = TextAnchor.MiddleCenter;
        ToastText.text = string.Empty;

        toastRoot.SetActive(false);
    }

    private IEnumerator ShowToastRoutine(string message)
    {
        var toastRoot = ToastText.transform.parent.gameObject;
        toastRoot.SetActive(true);
        ToastText.text = message;
        yield return new WaitForSeconds(2f);
        ToastText.text = string.Empty;
        toastRoot.SetActive(false);
        _toastCoroutine = null;
    }

    private void ResolveReferences()
    {
        AccountInputField ??= FindChildComponent<InputField>("AccountInputField");
        PasswordInputField ??= FindChildComponent<InputField>("PasswordInputField");
        AttachBtn ??= FindChildComponent<Button>("AttachBtn");
        LoginBtn ??= FindChildComponent<Button>("LoginBtn");
        CreateRoomBtn ??= FindChildComponent<Button>("CreateRoomBtn");
        JoinRoomBtn ??= FindChildComponent<Button>("JoinRoomBtn");
        ConnectBtn ??= FindChildComponent<Button>("ConnectBtn");
        ReadyBtn ??= FindChildComponent<Button>("ReadyBtn");
        ShutdownBtn ??= FindChildComponent<Button>("ShutdownBtn");
        ReplayBtn ??= FindChildComponent<Button>("ReplayBtn");
        SelectReplayDropdown ??= FindChildComponent<Dropdown>("SelectReplayDropdown");
        ToastText ??= FindChildComponent<Text>("ToastText");
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
