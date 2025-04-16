using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器
/// </summary>
public class GameManager : MManager<GameManager>
{
    /// <summary>
    /// 玩家ID
    /// </summary>
    public uint PlayerId;
    /// <summary>
    /// 当前加入的房间信息
    /// </summary>
    public RoomInfo RoomInfo;
    /// <summary>
    /// 当前服务器权威帧
    /// </summary>
    public int ServerAuthorityFrame = -1;
    /// <summary>
    /// 战斗是否已经连接
    /// </summary>
    public bool IsBattleConnected = false;
    /// <summary>
    /// 战斗是否已经开始
    /// </summary>
    public bool IsBattleStart = false;

    private FrameBuffer _frameBuffer;
    /// <summary>
    /// 帧缓存对象
    /// </summary>
    public FrameBuffer FrameBuffer
    {
        get => _frameBuffer;
        set => _frameBuffer = value;
    }
    /// <summary>
    /// 帧引擎
    /// </summary>
    private FrameEngine _frameEngine;
    /// <summary>
    /// 战斗控制器
    /// </summary>
    private BattleController _battleController;
    /// <summary>
    /// 回放控制器
    /// </summary>
    private ReplayController _replayController;
    /// <summary>
    /// 战斗显示对象
    /// </summary>
    private BattleView _battleView;
    
    /// <summary>
    /// 获取玩家战斗POS
    /// </summary>
    /// <returns>战斗POS</returns>
    public int GetBattlePos()
    {
        return (int)(PlayerId - GameSetting.DefaultPlayerIdBase - 1);
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        _battleController = new BattleController();
        _replayController = new ReplayController();
        _frameBuffer = new FrameBuffer(BattleSetting.MaxPlayerInRoomCount, BattleSetting.MaxFrameCount);
        _frameEngine = new FrameEngine();
        _frameEngine.RegisterNetUpdateListener(_battleController.NetUpdate);
        _frameEngine.RegisterFrameUpdateListener(_battleController.LogicUpdate);
        _frameEngine.RegisterReplayUpdateListener(_replayController.ReplayUpdate);
    }

    /// <summary>
    /// 开启战斗
    /// </summary>
    /// <param name="battleType">战斗类型</param>
    public void StartBattle(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
            {
                StartRemoteBattle();
                break;
            }
            case BattleType.Replay:
            {
                StartReplayBattle();
                break;
            }
        }
    }

    /// <summary>
    /// 开启多人战斗
    /// </summary>
    private void StartRemoteBattle()
    {
        _battleController.InitEntities();
        LoomManager.Instance.QueueOnMainThread(() => { _battleView.InitView(_battleController.DisplayBattleEntity); });
        InitializeEntitySystems();
        _frameEngine.StartNetEngine(BattleSetting.NetInterval);
        _frameEngine.StartFrameEngine(BattleSetting.BattleInterval);
        LoomManager.Instance.QueueOnMainThread(() => { BattleRecordManager.Instance.StartRecordBattle(GetBattlePos()); });
    }

    /// <summary>
    /// 开启回放
    /// </summary>
    private void StartReplayBattle()
    {
        _replayController.InitReplay(GetBattlePos());
        LoomManager.Instance.QueueOnMainThread(() => { _battleView.InitView(_replayController.DisplayBattleEntity); });
        InitializeEntitySystems();
        _frameEngine.StartReplayEngine(BattleSetting.BattleInterval);
        _replayController.StartReplayStopwatch();
    }
    
    /// <summary>
    /// 初始化实体系统
    /// </summary>
    private void InitializeEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Initialize), false);
    }

    /// <summary>
    /// 渲染轮询
    /// </summary>
    /// <param name="battleType">战斗类型</param>
    /// <param name="deltaTime">每帧时间</param>
    public void RenderUpdate(BattleType battleType, float deltaTime)
    {
        try
        {
            switch (battleType)
            {
                case BattleType.Remote:
                {
                    _battleView.RenderUpdate(_battleController.DisplayBattleEntity, deltaTime);
                    break;
                }
                case BattleType.Replay:
                {
                    _battleView.RenderUpdate(_replayController.DisplayBattleEntity, deltaTime);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,$"[RENDER] Exception ->\n{ex.Message}\n{ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// 停止战斗
    /// </summary>
    /// <param name="battleType">战斗类型</param>
    public void StopBattle(BattleType battleType)
    {
        switch (battleType)
        {
            case BattleType.Remote:
            {
                StopRemoteBattle();
                break;
            }
            case BattleType.Replay:
            {
                StopReplayBattle();
                break;
            }
        }
    }

    /// <summary>
    /// 停止多人战斗
    /// </summary>
    private void StopRemoteBattle()
    {
        if (!IsBattleStart)
            return;
        
        _frameEngine.StopEngine();
        _battleView.OnRelease(_battleController.DisplayBattleEntity);
        ReleaseEntitySystems();
        NetworkManager.Instance.KcpShutdown();
    }

    /// <summary>
    /// 停止回放
    /// </summary>
    private void StopReplayBattle()
    {
        _frameEngine.StopReplayEngine();
        _battleView.OnRelease(_replayController.DisplayBattleEntity);
        ReleaseEntitySystems();
    }

    private void CreateBattleView()
    {
        var go = new GameObject("BattleView");
        _battleView = Util.GetOrAddComponent<BattleView>(go);
    }

    /// <summary>
    /// 房间人满加载战斗场景
    /// </summary>
    public void OnRoomFull()
    {
        StartCoroutine(OnLoadBattleSceneAsync(() =>
        {
            CreateBattleView();
            CameraManager.Instance.ToggleBattleCamera();
            NetworkManager.Instance.KcpConnect();
            NetworkManager.Instance.KcpUpdate();
        }));
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator OnLoadBattleSceneAsync(Action callback)
    {
        yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)EGameScene.World, LoadSceneMode.Single);
        callback.Invoke();
    }

    /// <summary>
    /// 释放实体系统
    /// </summary>
    private void ReleaseEntitySystems()
    {
        Util.InvokeAttributeCall(this, typeof(EntitySystem), false, typeof(EntitySystem.Release), false);
    }

    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        if (_frameEngine != null)
        {
            _frameEngine.UnRegisterFrameUpdateListener();
            _frameEngine.UnRegisterNetUpdateListener();
            _frameEngine.UnRegisterReplayUpdateListener();
        }
    }
}