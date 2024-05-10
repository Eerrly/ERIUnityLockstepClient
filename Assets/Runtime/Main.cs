using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    public InputField AccountInputField;
    public InputField PasswordInputField;
    public Button LoginBtn;
    public Button CreateRoomBtn;
    public Button JoinRoomBtn;
    public Button ConnectBtn;
    public Button ReadyBtn;
    public Button ShutdownBtn;

    private void Awake()
    {
        InitLogger();
        LoomManager.Instance.Initialize();
        GameManager.Instance.Initialize();
        MsgPoolManager.Instance.Initialize();
        NetworkManager.Instance.Initialize();
        InputManager.Instance.Initialize();
    }

    private void InitLogger()
    {
        var currentLoggerPath = Path.Combine(Application.persistentDataPath, "game.log");
        Logger.Initialize(currentLoggerPath, new Logger());
        Logger.SetLoggerLevel((int)LogLevel.Error | (int)LogLevel.Info | (int)LogLevel.Warning);
        Logger.log = Debug.Log; 
        Logger.logError = Debug.LogError;
        Logger.logWarning = Debug.LogWarning;
    }

    private void Start()
    {
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
        });
        ShutdownBtn.onClick.AddListener(() =>
        {
            NetworkManager.Instance.KcpShutdown();
        });
        NetworkManager.Instance.TcpConnect();
    }

    private void Update()
    {
        if(!GameManager.Instance.IsBattleStart) return;
        
        GameManager.Instance.RenderUpdate(Time.deltaTime);
    }

    private void OnDestroy()
    {
        GameManager.Instance.StopBattle();
    }
}
