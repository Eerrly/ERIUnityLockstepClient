using System.Collections.Generic;

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

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Name:{Name} Frame:{Frame} Time:{Time} ");
        foreach (var p in PlayerEntities)
            sb.Append($"[ID:{p.ID} Yaw:{p.Input.yaw} Key:{p.Input.key}]");
        return sb.ToString();
    }

}