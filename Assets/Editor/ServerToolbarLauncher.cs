using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

/// <summary>
/// 在 Unity 顶部工具栏增加服务端状态和快捷启动按钮。
/// </summary>
[InitializeOnLoad]
public static class ServerToolbarLauncher
{
    private const string ContainerName = "ERIUnitySimpleServerToolbarContainer";
    private const string ServerDirectory = @"E:\GitProjects\LockStepWorkRoot\ERIUnitySimpleServer";
    private const string ServerProjectPath = ServerDirectory + @"\TestKCPServer.csproj";
    private const string ServerHost = "127.0.0.1";
    private const int TcpPort = 10085;
    private const string ServerProcessIdKey = "ERIUnitySimpleServer.ProcessId";

    private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    private static ScriptableObject currentToolbar;
    private static VisualElement statusDot;
    private static Label statusLabel;
    private static Label serverButton;
    private static Process serverProcess;
    private static double nextStatusCheckTime;
    private static bool cachedPortActive;

    static ServerToolbarLauncher()
    {
        RestoreTrackedProcess();
        EditorApplication.update += OnEditorUpdate;
        EditorApplication.quitting += StopTrackedServer;
    }

    [MenuItem("Tools/服务器/启动 ERIUnitySimpleServer")]
    public static void StartServerFromMenu()
    {
        StartServer();
    }

    [MenuItem("Tools/服务器/停止本次启动的服务器")]
    public static void StopServerFromMenu()
    {
        StopServer();
    }

    private static void OnEditorUpdate()
    {
        TryAttachToolbar();

        if (EditorApplication.timeSinceStartup >= nextStatusCheckTime)
        {
            nextStatusCheckTime = EditorApplication.timeSinceStartup + 1.0d;
            cachedPortActive = IsServerPortOpen();
            UpdateView();
        }
    }

    private static void TryAttachToolbar()
    {
        if (ToolbarType == null)
        {
            return;
        }

        if (currentToolbar == null)
        {
            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            currentToolbar = toolbars.Length > 0 ? toolbars[0] as ScriptableObject : null;
        }

        if (currentToolbar == null)
        {
            return;
        }

        var root = ToolbarType.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(currentToolbar) as VisualElement;
        if (root == null)
        {
            return;
        }

        var playModeZone = root.Q<VisualElement>("ToolbarZonePlayMode");
        if (playModeZone == null)
        {
            return;
        }

        if (playModeZone.Q(ContainerName) != null)
        {
            return;
        }

        var container = new VisualElement
        {
            name = ContainerName
        };
        container.style.flexDirection = FlexDirection.Row;
        container.style.alignItems = Align.Center;
        container.style.marginLeft = 8;
        container.style.marginRight = 4;
        container.style.height = 22;

        statusDot = new VisualElement();
        statusDot.style.width = 8;
        statusDot.style.height = 8;
        statusDot.style.marginLeft = 4;
        statusDot.style.marginRight = 5;
        statusDot.style.borderTopLeftRadius = 4;
        statusDot.style.borderTopRightRadius = 4;
        statusDot.style.borderBottomLeftRadius = 4;
        statusDot.style.borderBottomRightRadius = 4;

        statusLabel = new Label();
        statusLabel.style.fontSize = 12;
        statusLabel.style.marginRight = 8;
        statusLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

        serverButton = new Label();
        serverButton.RegisterCallback<MouseUpEvent>(evt =>
        {
            if (evt.button != 0)
            {
                return;
            }

            ToggleServer();
            evt.StopPropagation();
        });
        serverButton.style.height = 22;
        serverButton.style.minWidth = 96;
        serverButton.style.paddingLeft = 8;
        serverButton.style.paddingRight = 8;
        serverButton.style.unityTextAlign = TextAnchor.MiddleCenter;
        serverButton.style.fontSize = 12;
        serverButton.style.color = Color.white;
        serverButton.style.alignSelf = Align.Center;
        serverButton.style.justifyContent = Justify.Center;
        serverButton.style.backgroundColor = new Color(0.70f, 0.16f, 0.16f);
        serverButton.style.borderTopLeftRadius = 4;
        serverButton.style.borderTopRightRadius = 4;
        serverButton.style.borderBottomLeftRadius = 4;
        serverButton.style.borderBottomRightRadius = 4;
        serverButton.style.borderTopWidth = 0;
        serverButton.style.borderRightWidth = 0;
        serverButton.style.borderBottomWidth = 0;
        serverButton.style.borderLeftWidth = 0;

        container.Add(statusDot);
        container.Add(statusLabel);
        container.Add(serverButton);
        playModeZone.Add(container);
        UpdateView();
    }

    private static void ToggleServer()
    {
        if (IsServerActive())
        {
            StopServer();
        }
        else
        {
            StartServer();
        }
    }

    private static void StartServer()
    {
        if (IsServerActive())
        {
            Debug.Log("ERIUnitySimpleServer 已经启动，无需重复启动。");
            UpdateView();
            return;
        }

        if (!Directory.Exists(ServerDirectory) || !File.Exists(ServerProjectPath))
        {
            Debug.LogError($"找不到服务端项目：{ServerProjectPath}");
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k dotnet run --project \"{ServerProjectPath}\"",
                WorkingDirectory = ServerDirectory,
                UseShellExecute = true
            };

            serverProcess = Process.Start(startInfo);
            if (serverProcess != null)
            {
                SessionState.SetInt(ServerProcessIdKey, serverProcess.Id);
            }

            Debug.Log($"已启动 ERIUnitySimpleServer：{ServerProjectPath}");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            cachedPortActive = IsServerPortOpen();
            UpdateView();
        }
    }

    private static void StopServer()
    {
        if (!IsTrackedProcessRunning())
        {
            Debug.LogWarning("检测到服务器可能已启动，但它不是由当前 Unity 按钮启动的进程，无法安全停止。");
            UpdateView();
            return;
        }

        StopTrackedServer();
        cachedPortActive = IsServerPortOpen();
        UpdateView();
    }

    private static void StopTrackedServer()
    {
        if (!IsTrackedProcessRunning())
        {
            return;
        }

        try
        {
            // cmd 窗口会作为父进程启动 dotnet，使用 taskkill /T 才能同时清理子进程。
            Process.Start(new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/PID {serverProcess.Id} /T /F",
                UseShellExecute = false,
                CreateNoWindow = true
            })?.WaitForExit(3000);

            serverProcess.Dispose();
            serverProcess = null;
            SessionState.EraseInt(ServerProcessIdKey);
            Debug.Log("已停止当前 Unity 编辑器启动的 ERIUnitySimpleServer。");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void RestoreTrackedProcess()
    {
        var processId = SessionState.GetInt(ServerProcessIdKey, -1);
        if (processId <= 0)
        {
            return;
        }

        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                serverProcess = process;
            }
        }
        catch
        {
            SessionState.EraseInt(ServerProcessIdKey);
        }
    }

    private static bool IsServerActive()
    {
        return IsTrackedProcessRunning() || cachedPortActive;
    }

    private static bool IsTrackedProcessRunning()
    {
        return serverProcess != null && !serverProcess.HasExited;
    }

    private static bool IsServerPortOpen()
    {
        try
        {
            // 只查询本机监听端口，不主动连接服务器，避免服务端误打印健康检查连接。
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .Any(endpoint => endpoint.Port == TcpPort);
        }
        catch
        {
            return false;
        }
    }

    private static void UpdateView()
    {
        if (statusDot == null || statusLabel == null || serverButton == null)
        {
            return;
        }

        var active = IsServerActive();
        statusDot.style.backgroundColor = active ? new Color(0.07f, 0.78f, 0.38f) : new Color(0.86f, 0.18f, 0.18f);
        statusLabel.text = active ? "Server Active" : "Server Offline";
        statusLabel.tooltip = active
            ? $"服务器已启动：{ServerHost}:{TcpPort}"
            : $"服务器未启动：{ServerHost}:{TcpPort}";

        serverButton.text = active ? "Stop Server" : "Start Server";
        serverButton.tooltip = active
            ? "停止服务器"
            : "启动服务器";
        serverButton.style.backgroundColor = active ? new Color(0.70f, 0.16f, 0.16f) : new Color(0.16f, 0.36f, 0.62f);
        serverButton.style.color = Color.white;
    }
}
