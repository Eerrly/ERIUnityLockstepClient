using System.Collections.Generic;
using System.IO;

public class BattleEntity: BaseEntity
{
    public string Name;

    public int Frame;
    
    public float Time;

    public List<PlayerEntity> PlayerEntities = new List<PlayerEntity>();

    public override void Init()
    {
        Name = "None";
        Frame = -1;
        Time = 0f;
        PlayerEntities = new List<PlayerEntity>();
    }

    public override void Reset()
    {
        Name = "None";
        Frame = -1;
        Time = 0f;
        PlayerEntities = new List<PlayerEntity>();
    }

    public override void CopyTo(BaseEntity entity)
    {
        var battleEntity = entity as BattleEntity;
        battleEntity.Frame = Frame;
        battleEntity.Time = Time;
        for (int i = 0; i < PlayerEntities.Count; i++)
        {
            if(i >= battleEntity.PlayerEntities.Count) battleEntity.PlayerEntities.Add(new PlayerEntity());
            PlayerEntities[i].CopyTo(battleEntity.PlayerEntities[i]);
        }
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(Frame);
        writer.Write(PlayerEntities.Count);
        foreach (var entity in PlayerEntities)
            entity.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        Frame = reader.ReadInt32();
        var playerCount = reader.ReadInt32();
        for (var i = 0; i < playerCount; i++)
        {
            if(PlayerEntities.Count <= i) PlayerEntities.Add(new PlayerEntity());
            PlayerEntities[i].Deserialize(reader);
        }
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