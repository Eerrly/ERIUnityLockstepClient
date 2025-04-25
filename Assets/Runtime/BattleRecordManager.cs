using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// 战斗收集管理器
/// </summary>
public class BattleRecordManager : AManager<BattleRecordManager>
{
    /// <summary>
    /// 收集文件IO流
    /// </summary>
    private FileStream _frameFileStream;
    private BinaryWriter _frameBinaryWriter;
    private Thread _writerThread;
    /// <summary>
    /// 当前正在收集的战斗实体队列
    /// </summary>
    private readonly RingBuffer<BattleEntity> _battleEntities = new(32);
    /// <summary>
    /// 战斗实体缓存集合
    /// </summary>
    private List<BattleEntity> _unusedBattleEntities;
    
    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        _unusedBattleEntities = new List<BattleEntity>();
    }

    /// <summary>
    /// 释放
    /// </summary>
    public override void OnRelease()
    {
        try
        {
            if (_frameFileStream != null)
            {
                _frameFileStream.Close();
                _frameBinaryWriter.Close();
                _frameFileStream = null;
                _frameBinaryWriter = null;
            }

            if (_writerThread != null)
            {
                _writerThread.Join();
                _writerThread = null;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"{ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// 开始战斗收集
    /// </summary>
    /// <param name="pos">玩家战斗POS</param>
    public void StartRecordBattle(int pos)
    {
        var battleRecordPath = $"{Application.persistentDataPath}/battle_record_{pos}.log";
        _frameFileStream = new FileStream(battleRecordPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        _frameBinaryWriter = new BinaryWriter(_frameFileStream);
        _writerThread = new Thread(new ThreadStart(OnRecordBattleThread));
        _writerThread.Start();
    }

    /// <summary>
    /// 战斗收集线程
    /// </summary>
    private void OnRecordBattleThread()
    {
        while (_frameFileStream != null)
        {
            try
            {
                while (_battleEntities.TryDequeue(out var entity))
                {
                    if (entity == null) 
                        continue;
                    entity.Serialize(_frameBinaryWriter);
                    lock (_unusedBattleEntities) _unusedBattleEntities.Add(entity);
                }
                _frameBinaryWriter.Flush();
            }
            catch (Exception ex)
            {
                if (_frameFileStream != null)
                {
                    _frameFileStream.Dispose();
                    _frameFileStream = null;
                }
                Logger.Log(LogLevel.Error, $"{ex.Message}\n{ex.StackTrace}");
                break;
            }
            Thread.Sleep(BattleSetting.BattleInterval / 2);
        }
    }

    /// <summary>
    /// 收集战斗实体
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
    public void RecordBattleEntity(BattleEntity battleEntity)
    {
        try
        {
            if (_frameFileStream == null) 
                return;
            
            BattleEntity entity = null;
            lock (_unusedBattleEntities)
            {
                if (_unusedBattleEntities.Count > 0)
                {
                    entity = _unusedBattleEntities[^1];
                    _unusedBattleEntities.RemoveAt(_unusedBattleEntities.Count - 1);
                }
            }

            entity ??= new BattleEntity();

            entity.BattleEntityType = EBattleEntityType.Record;
            battleEntity.CopyTo(entity);
            _battleEntities.Enqueue(entity);
        }
        catch (Exception ex)
        {
            if (_frameFileStream != null)
            {
                _frameFileStream.Dispose();
                _frameFileStream = null;
            }
            Logger.Log(LogLevel.Error, $"{ex.Message}\n{ex.StackTrace}");
        }
    }
    
}