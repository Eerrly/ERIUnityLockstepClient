using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏总入口
/// </summary>
public class Main : MonoBehaviour
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

    private string[] replayPaths;
    public Dropdown SelectReplayDropdown;

    /// <summary>
    /// 当前战斗类型
    /// </summary>
    private BattleType currBattleType = BattleType.Remote;

    /// <summary>
    /// 初始化
    /// 1. 初始化日志打印工具
    /// 2. 初始化多线程调度器
    /// 3. 初始化消息池
    /// 4. 初始化网络模块
    /// 5. 初始化输入模块
    /// 6. 初始化战斗记录模块
    /// 7. 初始化相机模块
    /// 8. 初始化动画模块
    /// </summary>
    private void Awake()
    {
        InitLogger();
        LoomManager.Instance.Initialize();
        GameManager.Instance.Initialize();
        MsgPoolManager.Instance.Initialize();
        NetworkManager.Instance.Initialize();
        InputManager.Instance.Initialize();
        BattleRecordManager.Instance.Initialize();
        CameraManager.Instance.Initialize();
        AnimationManager.Instance.Initialize();
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
            if (replayPaths.Length <= 0)
                return;
            var matches = System.Text.RegularExpressions.Regex.Matches(SelectReplayDropdown.options[SelectReplayDropdown.value].text, @"battle_record_(\d+).log");
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
            var tmpData = new Dropdown.OptionData
            {
                text = Path.GetFileName(path)
            };
            SelectReplayDropdown.options.Add(tmpData);
        }
        SelectReplayDropdown.captionText.text = Path.GetFileName(replayPaths[0]);
    }

    private void Update()
    {
        if(!GameManager.Instance.IsBattleStart) return;
        
        GameManager.Instance.RenderUpdate(currBattleType, Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (BattleRecordManager.Instance != null) BattleRecordManager.Instance.OnRelease();
        if (GameManager.Instance != null) GameManager.Instance.StopBattle(currBattleType);
        if (GameManager.Instance != null) GameManager.Instance.OnRelease();
        if (NetworkManager.Instance != null) NetworkManager.Instance.OnRelease();
    }
}
