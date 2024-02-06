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

    private void Start()
    {
        connectBtn.onClick.AddListener(() => { NetworkManager.Instance.TcpConnect(); });
        disconnectBtn.onClick.AddListener(() => { NetworkManager.Instance.CloseKcpClient(); });
        
        loginBtn.onClick.AddListener(() => { GameManager.Instance.Login(); });
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
                if (GameManager.Instance.Status.Contains(GameManager.Instance.PlayerId)) return;
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

    private void Update()
    {
        tcpConnectionStateTxt.text = NetworkManager.Instance.TcpConnected ? "Connected" : "Disconnected";
        GameManager.Instance.Account = accountInputField.text;
        GameManager.Instance.Password = passwordInputField.text;

        loginTxt.text = GameManager.Instance.PlayerId == default ? "Login" : "Logged";
        if (GameManager.Instance.PlayerId != default)
        {
            loginBtn.interactable = false;
            playerIdTxt.text = GameManager.Instance.PlayerId.ToString();
        }

        createRoomBtn.interactable = GameManager.Instance.RoomIdList.Count <= 0;
        if (GameManager.Instance.RoomIdList.Count > 0)
        {
            var roomId = GameManager.Instance.RoomIdList.First();
            
            stringBuilder.Clear();
            stringBuilder.Append($"{roomId} [");
            if (GameManager.Instance.RoomInfoDic.TryGetValue(roomId, out var playerIds))
            {
                foreach (var id in playerIds)
                {
                    if (id == GameManager.Instance.PlayerId) isJoined = true;
                    stringBuilder.Append(id);
                    stringBuilder.Append(',');
                }
                stringBuilder.Append("]");
            }
            roomInfoTxt.text = stringBuilder.ToString();
            joinRoomTxt.text = isJoined ? "Joined" : "Join";
            joinRoomBtn.interactable = !isJoined;
        }

        kcpConnectionStateTxt.text = $"KCP Connection State: {(NetworkManager.Instance.KcpConnected ? "Connected" : "Disconnected")}";
        if (NetworkManager.Instance.KcpConnected)
        {
            kcpConnectionStateTxt.text = $"KCP Connection State: {(NetworkManager.Instance.KcpConnected ? "Connected" : "Disconnected")} RoomId:{GameManager.Instance.RoomId}";
            for (int i = 0; i < GameManager.Instance.RoomInfoDic[GameManager.Instance.RoomId].Count; i++)
            {
                var playerId = GameManager.Instance.RoomInfoDic[GameManager.Instance.RoomId][i];
                var isReadied = GameManager.Instance.Status.Contains(playerId);
                playerReadyInfoTxt[i].text = isReadied ? $"PlayerId:{playerId} Readied" : $"PlayerId:{playerId} Waiting...";
                playerReadyBtn[i].gameObject.SetActive(GameManager.Instance.PlayerId == playerId);
                playerReadyBtn[i].interactable = !isReadied;
            }
        }

        if (!GameManager.Instance.IsBattleStart || GameManager.Instance.ServerAuthorityFrame == -1) return;
        
        stringBuilder.Clear();
        if (GameManager.Instance.FrameInfoDic.TryGetValue(GameManager.Instance.ServerAuthorityFrame, out var datum))
        {
            foreach (var d in datum) stringBuilder.Append($"Data:{d}").Append(",");
        }

        var displayFrame = GameManager.Instance.BattleController.DisplayEntity.Frame;
        frameInfoTxt.text = $"ClientFrame:{displayFrame} " +
                            $"ServerFrame:{GameManager.Instance.ServerAuthorityFrame} " +
                            $"Offset:{GameManager.Instance.ServerAuthorityFrame - displayFrame} " +
                            $"RealPing:{NetworkManager.Instance.RealPing}" +
                            $"MinPing:{NetworkManager.Instance.MinPing}" +
                            $"Datum:{stringBuilder}";
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnRelease();
        NetworkManager.Instance.OnRelease();
    }
}