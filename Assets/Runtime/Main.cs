using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏总入口
/// </summary>
public class Main : MonoBehaviour
{
    private static Main _instance;

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

    private string[] replayPaths;
    public Dropdown SelectReplayDropdown;

    /// <summary>
    /// 当前战斗类型
    /// </summary>
    private BattleType currBattleType = BattleType.Remote;

    private Text _toastText;
    private Coroutine _toastCoroutine;

    /// <summary>
    /// 初始化
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        InitLogger();
        LoomManager.Instance.Initialize();
        GameManager.Instance.Initialize();
        MsgPoolManager.Instance.Initialize();
        NetworkManager.Instance.Initialize();
        InputManager.Instance.Initialize();
        BattleRecordManager.Instance.Initialize();
        CameraManager.Instance.Initialize();
        AnimationManager.Instance.Initialize();
        GameManager.Instance.OnReplayCompleted += ShowToast;
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// 初始化日志打印工具
    /// </summary>
    private void InitLogger()
    {
        var currentLoggerPath = Path.Combine(Application.persistentDataPath, "game.log");
        Logger.Initialize(currentLoggerPath, new Logger());
#if DEBUG_MODEL
        Logger.SetLoggerLevel((int)LogLevel.Error | (int)LogLevel.Info | (int)LogLevel.Warning);
#else
        Logger.SetLoggerLevel((int)LogLevel.Error);
#endif
        Logger.log = Debug.Log;
        Logger.logError = Debug.LogError;
        Logger.logWarning = Debug.LogWarning;
    }

    private void Start()
    {
        CameraManager.Instance.ToggleUICamera();
        CameraManager.Instance.SetCanvasUICamera(gameObject);
        EnsureToast();

        AttachBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.TcpConnect();
        });
        LoginBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SendLogicLoginMessage(AccountInputField.text, PasswordInputField.text);
        });
        CreateRoomBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SendLogicCreateRoomMessage(GameManager.Instance.PlayerId);
        });
        JoinRoomBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SendLogicJoinRoomMessage(GameManager.Instance.RoomInfo.RoomId, GameManager.Instance.PlayerId);
        });
        ConnectBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SendBattleConnectMessage(GameManager.Instance.PlayerId, 2);
        });
        ReadyBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.SendBattleReadyMessage(GameManager.Instance.RoomInfo.RoomId, GameManager.Instance.PlayerId);
            CameraManager.Instance.ToggleUICamera();
        });
        ShutdownBtn.onClick.AddListener(() =>
        {
            GameManager.Instance.StopBattle(currBattleType);
        });

        UpdateReplayDropDownInfo();
        ReplayBtn.onClick.AddListener(() =>
        {
            if (replayPaths == null || replayPaths.Length <= 0 || SelectReplayDropdown.options.Count <= 0)
                return;

            var matches = Regex.Matches(SelectReplayDropdown.options[SelectReplayDropdown.value].text, @"battle_record_(\d+).log");
            if (matches.Count <= 0)
                return;

            if (uint.TryParse(matches[0].Groups[1].Value, out var pos))
            {
                GameManager.Instance.PlayerId = pos + GameSetting.DefaultPlayerIdBase + 1;

                currBattleType = BattleType.Replay;
                GameManager.Instance.StartBattle(currBattleType);
                GameManager.Instance.IsBattleStart = true;
            }
        });
    }

    private void UpdateReplayDropDownInfo()
    {
        SelectReplayDropdown.options.Clear();
        replayPaths = Directory.GetFiles(Application.persistentDataPath, "*.log");
        foreach (var path in replayPaths)
        {
            var fileName = Path.GetFileName(path);
            if (!fileName.StartsWith("battle_record_"))
                continue;

            SelectReplayDropdown.options.Add(new Dropdown.OptionData
            {
                text = fileName
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
                text = "No replays found"
            });
            SelectReplayDropdown.value = 0;
            SelectReplayDropdown.RefreshShownValue();
            ReplayBtn.interactable = false;
        }
    }

    private void EnsureToast()
    {
        if (_toastText != null)
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

        _toastText = textGo.GetComponent<Text>();
        _toastText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _toastText.fontSize = 24;
        _toastText.color = Color.white;
        _toastText.alignment = TextAnchor.MiddleCenter;
        _toastText.text = string.Empty;

        toastRoot.SetActive(false);
    }

    private void ShowToast(string message)
    {
        EnsureToast();
        if (_toastCoroutine != null)
            StopCoroutine(_toastCoroutine);
        _toastCoroutine = StartCoroutine(ShowToastRoutine(message));
    }

    private IEnumerator ShowToastRoutine(string message)
    {
        var toastRoot = _toastText.transform.parent.gameObject;
        CameraManager.Instance.SetCanvasUICamera(gameObject);
        toastRoot.SetActive(true);
        _toastText.text = message;
        yield return new WaitForSeconds(2f);
        _toastText.text = string.Empty;
        toastRoot.SetActive(false);
        _toastCoroutine = null;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsBattleStart) return;

        GameManager.Instance.RenderUpdate(currBattleType, Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
        if (GameManager.Instance != null) GameManager.Instance.OnReplayCompleted -= ShowToast;
        if (BattleRecordManager.Instance != null) BattleRecordManager.Instance.OnRelease();
        if (GameManager.Instance != null) GameManager.Instance.StopBattle(currBattleType);
        if (GameManager.Instance != null) GameManager.Instance.OnRelease();
        if (NetworkManager.Instance != null) NetworkManager.Instance.OnRelease();
    }
}
