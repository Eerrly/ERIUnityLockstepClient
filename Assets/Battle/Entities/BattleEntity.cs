using System.Collections.Generic;
using System.IO;

/// <summary>
/// 战斗实体
/// </summary>
public class BattleEntity: BaseEntity
{
    /// <summary>
    /// 实体名称
    /// </summary>
    public string Name;

    /// <summary>
    /// 帧号
    /// </summary>
    public int Frame;
    
    /// <summary>
    /// 时间
    /// </summary>
    public FixedNumber Time;

    /// <summary>
    /// 玩家实体列表
    /// </summary>
    public List<PlayerEntity> PlayerEntities = new List<PlayerEntity>();
    
    /// <summary>
    /// 状态组件
    /// </summary>
    public readonly StateComponent State = new StateComponent();

    public override void Init()
    {
        Name = "None";
        Frame = -1;
        Time = FixedNumber.Zero;
        PlayerEntities = new List<PlayerEntity>();

        State.prevStateId = (int)EBattleState.None;
        State.currStateId = (int)EBattleState.None;
        State.nextStateId = (int)EBattleState.Playing;
        State.count = (int)EBattleState.Count;
    }

    public override void Reset()
    {
        Name = "None";
        Frame = -1;
        Time = FixedNumber.Zero;
        PlayerEntities = new List<PlayerEntity>();
        
        State.prevStateId = (int)EBattleState.None;
        State.currStateId = (int)EBattleState.None;
        State.nextStateId = (int)EBattleState.Playing;
        State.count = (int)EBattleState.Count;
    }

    /// <summary>
    /// 通过玩家ID获取玩家实体
    /// </summary>
    /// <param name="playerId">玩家ID</param>
    /// <returns>玩家实体</returns>
    public PlayerEntity FindPlayerEntity(int playerId)
    {
        if (playerId >= 0)
        {
            for (var i = 0; i < PlayerEntities.Count; i++)
            {
                if (PlayerEntities[i].ID == playerId)
                    return PlayerEntities[i];
            }
        }
        return null;
    }

    public override void CopyTo(BaseEntity entity)
    {
        var battleEntity = entity as BattleEntity;
        battleEntity.Frame = Frame;
        battleEntity.Time = Time;
        for (var i = 0; i < PlayerEntities.Count; i++)
        {
            if(i >= battleEntity.PlayerEntities.Count) battleEntity.PlayerEntities.Add(new PlayerEntity());
            PlayerEntities[i].CopyTo(battleEntity.PlayerEntities[i]);
        }
        State.CopyTo(battleEntity.State);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(Frame);
        writer.Write(Time._raw);
        writer.Write(PlayerEntities.Count);
        foreach (var entity in PlayerEntities)
            entity.Serialize(writer);
        base.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        Frame = reader.ReadInt32();
        Time = new FixedNumber(reader.ReadInt64());
        var playerCount = reader.ReadInt32();
        for (var i = 0; i < playerCount; i++)
        {
            if(PlayerEntities.Count <= i) PlayerEntities.Add(new PlayerEntity());
            PlayerEntities[i].Deserialize(reader);
        }
        base.Deserialize(reader);
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("[");
        sb.Append($"Name:{Name} Frame:{Frame} Time:{Time} ");
        foreach (var p in PlayerEntities)
            sb.Append($"{p}");
        sb.Append("]");
        return sb.ToString();
    }

}