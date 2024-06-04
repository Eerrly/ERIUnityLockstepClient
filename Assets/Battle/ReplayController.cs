using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ReplayController
{
    private int _frame;
    private readonly Stopwatch _stopwatch;
    private readonly List<BattleEntity> _battleEntities;
    private readonly BattleEntity _displayBattleEntity;
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

    public void StartReplayStopwatch()
    {
        _stopwatch.Start();
    }

    public void InitReplay(int pos)
    {
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