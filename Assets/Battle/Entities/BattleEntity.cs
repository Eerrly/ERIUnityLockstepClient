using System.Collections.Generic;
using System.IO;

public class BattleEntity: BaseEntity
{
    public string Name;

    public int Frame;
    
    public FixedNumber Time;

    public List<PlayerEntity> PlayerEntities = new List<PlayerEntity>();
    
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