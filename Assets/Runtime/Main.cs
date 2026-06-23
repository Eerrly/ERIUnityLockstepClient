using System.IO;
using UnityEngine;

/// <summary>
/// 游戏总入口
/// </summary>
public class Main : MonoBehaviour
{
    private static Main _instance;

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
        UIRootManager.Instance.Initialize();
        AnimationManager.Instance.Initialize();
        DontDestroyOnLoad(gameObject);
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
        CameraManager.Instance.ApplyMainCameraState();
        UIRootManager.Instance.ShowRoot(UIRootType.Modern);
    }

    private void Update()
    {
        GameManager.Instance.RenderUpdate(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (_instance != this)
            return;

        var gameManager = GameManager.InstanceOrNull;
        var battleRecordManager = BattleRecordManager.InstanceOrNull;
        var networkManager = NetworkManager.InstanceOrNull;
        var uiRootManager = UIRootManager.InstanceOrNull;
        var cameraManager = CameraManager.InstanceOrNull;

        gameManager?.StopBattle();
        battleRecordManager?.OnRelease();
        gameManager?.OnRelease();
        networkManager?.OnRelease();
        uiRootManager?.OnRelease();
        cameraManager?.OnRelease();

        _instance = null;
    }
}
