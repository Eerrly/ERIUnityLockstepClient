using System;
using System.IO;
using System.Linq;
using System.Text;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    private StringBuilder stringBuilder = new StringBuilder();

    public Button connectBtn;
    public Button disconnectBtn;
    public Text tcpConnectionStateTxt;
    public InputField accountInputField;
    public InputField passwordInputField;
    public Button loginBtn;
    public Text loginTxt;
    public Text playerIdTxt;
    public Button createRoomBtn;
    public Text roomInfoTxt;
    public Button joinRoomBtn;
    public Text joinRoomTxt;

    public Text kcpConnectionStateTxt;
    public Text[] playerReadyInfoTxt;
    public Button[] playerReadyBtn;
    public Text frameInfoTxt;

    public Button button1;
    public Button button2;
    public Button button3;
    public Button button4;

    private bool isJoined = false;
    private void Awake()
    {
        Application.runInBackground = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        
        InitLogger();
        BufferPool.InitPool(2, 512, 5, 5);
        
        GameManager.Instance.Initialize();
        NetworkManager.Instance.Initialize();
    }

    private string account;
    private string password;
    private void Start()
    {
        connectBtn.onClick.AddListener(() => { NetworkManager.Instance.TcpConnect(); });
        disconnectBtn.onClick.AddListener(() => { NetworkManager.Instance.CloseKcpClient(); });
        
        loginBtn.onClick.AddListener(() => { GameManager.Instance.Login(account, password); });
        createRoomBtn.onClick.AddListener(() => { GameManager.Instance.CreateRoom(); });
        
        joinRoomBtn.onClick.AddListener(() =>
        {
            if(isJoined) return;
            GameManager.Instance.JoinRoom();
        });

        for (int i = 0; i < playerReadyBtn.Length; i++)
        {
            playerReadyBtn[i].onClick.AddListener(() =>
            {
                var player = GameManager.Instance.GetPlayer();
                if (GameManager.Instance.TryGetRoom(player.RoomId, out var room) && room.Status.Contains(player.ID)) return;
                GameManager.Instance.RoomReady();
            });
        }
        
        button1.onClick.AddListener(() => { GameManager.Instance.SetFrame(1); });
        button2.onClick.AddListener(() => { GameManager.Instance.SetFrame(2); });
        button3.onClick.AddListener(() => { GameManager.Instance.SetFrame(3); });
        button4.onClick.AddListener(() => { GameManager.Instance.SetFrame(4); });
    }

    /// <summary>
    /// 初始化Logger
    /// </summary>
    void InitLogger()
    {
        var currentLoggerPath = Path.Combine(Application.persistentDataPath, "game.log");
        Debug.Log($"InitLogger currentLoggerPath: {currentLoggerPath}");
        Logger.Initialize(currentLoggerPath, new Logger());
        Logger.SetLoggerLevel((int)LogLevel.Info | (int)LogLevel.Warning | (int)LogLevel.Error | (int)LogLevel.Exception);
        Logger.log = Debug.Log;
        Logger.logError = Debug.LogError;
        Logger.logWarning = Debug.LogWarning;
    }

    private int _lastGetFrameIndex = -1;
    private void Update()
    {
        tcpConnectionStateTxt.text = NetworkManager.Instance.TcpConnected ? "Connected" : "Disconnected";
        account = accountInputField.text;
        password = passwordInputField.text;

        var player = GameManager.Instance.GetPlayer();
        loginTxt.text = player != null && player.ID == default ? "Login" : "Logged";
        if (player != null && player.ID != default)
        {
            loginBtn.interactable = false;
            playerIdTxt.text = player.ID.ToString();
        }

        var roomList = GameManager.Instance.GetRoomIdList();
        createRoomBtn.interactable = roomList.Count <= 0;

        RoomInfo room = null;
        if (roomList.Count > 0)
        {
            room = roomList.First();
            
            stringBuilder.Clear();
            stringBuilder.Append($"{room.ID} [");
            foreach (var id in room.PlayerIds)
            {
                if (id == player.ID) isJoined = true;
                stringBuilder.Append(id);
                stringBuilder.Append(',');
            }
            stringBuilder.Append("]");
            roomInfoTxt.text = stringBuilder.ToString();
            joinRoomTxt.text = isJoined ? "Joined" : "Join";
            joinRoomBtn.interactable = !isJoined;
        }

        kcpConnectionStateTxt.text = $"KCP Connection State: {(NetworkManager.Instance.KcpConnected ? "Connected" : "Disconnected")}";
        if (NetworkManager.Instance.KcpConnected && room != null)
        {
            kcpConnectionStateTxt.text = $"KCP Connection State: {(NetworkManager.Instance.KcpConnected ? "Connected" : "Disconnected")} RoomId:{player.RoomId}";
            for (int i = 0; i < room.PlayerIds.Count; i++)
            {
                var playerId = room.PlayerIds[i];
                var isReadied = room.Status.Contains(playerId);
                playerReadyInfoTxt[i].text = isReadied ? $"PlayerId:{playerId} Readied" : $"PlayerId:{playerId} Waiting...";
                playerReadyBtn[i].gameObject.SetActive(player.ID == playerId);
                playerReadyBtn[i].interactable = !isReadied;
            }
        }

        if (!GameManager.Instance.IsBattleStart || GameManager.Instance.ServerAuthorityFrame == -1) return;
        
        stringBuilder.Clear();

        var displayEntity = GameManager.Instance.GetDisplayEntity();
        frameInfoTxt.text = $"ServerFrame:{GameManager.Instance.ServerAuthorityFrame} " +
                            $"Offset:{GameManager.Instance.ServerAuthorityFrame - displayEntity.Frame} " +
                            $"RealPing:{NetworkManager.Instance.RealPing}" +
                            $"MinPing:{NetworkManager.Instance.MinPing}\n" +
                            $"Entity:{displayEntity}";
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnRelease();
        NetworkManager.Instance.OnRelease();
    }
}