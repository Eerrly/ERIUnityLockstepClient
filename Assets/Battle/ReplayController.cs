using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 回放控制器
/// </summary>
public class ReplayController
{
    /// <summary>
    /// 当前帧
    /// </summary>
    private int _frame;
    private readonly Stopwatch _stopwatch;
    /// <summary>
    /// 缓存战斗实体列表
    /// </summary>
    private readonly List<BattleEntity> _battleEntities;
    
    private readonly BattleEntity _displayBattleEntity;
    /// <summary>
    /// 渲染实体
    /// </summary>
    public BattleEntity DisplayBattleEntity => _displayBattleEntity;

    public ReplayController()
    {
        _frame = 0;
        _stopwatch = new Stopwatch();
        _battleEntities = new List<BattleEntity>();
        _displayBattleEntity = new BattleEntity();
        _displayBattleEntity.PlayerEntities.Add(new PlayerEntity(){ ID = 0 });
        _displayBattleEntity.PlayerEntities.Add(new PlayerEntity(){ ID = 1 });
    }

    /// <summary>
    /// 开始客户端计时器
    /// </summary>
    public void StartReplayStopwatch()
    {
        _stopwatch.Start();
    }

    /// <summary>
    /// 初始化回放数据
    /// </summary>
    /// <param name="pos">战斗POS</param>
    public void InitReplay(int pos)
    {
        // 最近的一次多人战斗记录
        var battleRecordPath = $"{Application.persistentDataPath}/battle_record_{pos}.log";
        if (!File.Exists(battleRecordPath))
        {
            Logger.Log(LogLevel.Error, $"InitReplay file not found: {battleRecordPath}");
            return;
        }

        try
        {
            using (var fs = new FileStream(battleRecordPath, FileMode.Open, FileAccess.Read))
            using (var br = new BinaryReader(fs))
            {
                while (fs.Position < fs.Length)
                {
                    var entity = new BattleEntity { Name = "Display" };
                    entity.Deserialize(br);
                    _battleEntities.Add(entity);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"InitReplay Deserialize Failed: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 渲染回放
    /// </summary>
    /// <param name="cancellationToken">任务取消句柄</param>
    /// <returns>任务</returns>
    public Task ReplayUpdate(CancellationToken cancellationToken)
    {
        if (_frame < _battleEntities.Count && _stopwatch.ElapsedMilliseconds >= _frame * BattleSetting.BattleInterval)
        {
            _battleEntities[_frame].CopyTo(_displayBattleEntity);
            _frame++;
        }
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }
    
}