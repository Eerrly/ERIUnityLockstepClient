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
    /// 当前正在手机的战斗实体队列
    /// </summary>
    private Queue<BattleEntity> _battleEntities;
    /// <summary>
    /// 战斗实体缓存集合
    /// </summary>
    private List<BattleEntity> _unusedBattleEntities;
    
    private static object _writerLock = new object();

    /// <summary>
    /// 初始化
    /// </summary>
    public override void Initialize()
    {
        _battleEntities = new Queue<BattleEntity>();
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
                while (_battleEntities.Count > 0)
                {
                    BattleEntity entity = null;
                    lock (_writerLock)
                    {
                        entity = _battleEntities.Dequeue();
                    }
                    if (entity != null)
                    {
                        entity.Serialize(_frameBinaryWriter);
                        _frameBinaryWriter.Flush();
                        lock (_writerLock)
                        {
                            _unusedBattleEntities.Add(entity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"{ex.Message}\n{ex.StackTrace}");
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
            if (_frameFileStream != null)
            {
                BattleEntity entity = null;
                lock (_writerLock)
                {
                    if (_unusedBattleEntities.Count > 0)
                    {
                        entity = _unusedBattleEntities[_unusedBattleEntities.Count - 1];
                        _unusedBattleEntities.RemoveAt(_unusedBattleEntities.Count - 1);
                    }
                }
                if (entity == null) entity = new BattleEntity();

                entity.Name = "Record";
                battleEntity.CopyTo(entity);
                lock (_writerLock)
                {
                    _battleEntities.Enqueue(entity);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"{ex.Message}\n{ex.StackTrace}");
        }
    }
    
}