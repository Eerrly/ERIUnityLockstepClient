using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ReplayController
{
    private readonly Stopwatch _stopwatch;
    private readonly List<BattleEntity> _battleEntities;
    private int _frame;
    private BattleEntity _displayBattleEntity;
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
        using (var fs = new FileStream(battleRecordPath, FileMode.Open, FileAccess.Read))
        using (var br = new BinaryReader(fs))
        {
            try
            {
                while (true)
                {
                    var entity = new BattleEntity() { Name = "Display" };
                    entity.Deserialize(br);
                    _battleEntities.Add(entity);
                }
            }
            catch (Exception _)
            {
                // ignored
            }
        }
    }

    public Task ReplayUpdate(CancellationToken cancellationToken)
    {
        if (_stopwatch.ElapsedMilliseconds >= _frame * BattleSetting.BattleInterval)
        {
            _battleEntities[_frame].CopyTo(_displayBattleEntity);
            _frame++;
        }
        return cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    }
    
}