using System.Collections.Generic;

public class BattleEntity: BaseEntity
{
    public int Frame = -1;

    public List<PlayerEntity> PlayerEntities = new List<PlayerEntity>();

    public override void Init()
    {
        Frame = -1;
        PlayerEntities = new List<PlayerEntity>();
    }

    public override void Reset()
    {
        Frame = -1;
        PlayerEntities = new List<PlayerEntity>();
    }

    public override void CopyTo(BaseEntity entity)
    {
        var battleEntity = entity as BattleEntity;
        battleEntity.Frame = Frame;
        for (int i = 0; i < PlayerEntities.Count; i++)
        {
            if(i >= battleEntity.PlayerEntities.Count) battleEntity.PlayerEntities.Add(new PlayerEntity());
            PlayerEntities[i].CopyTo(battleEntity.PlayerEntities[i]);
        }
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"Frame:{Frame} ");
        foreach (var p in PlayerEntities)
            sb.Append($"[ID:{p.ID} Yaw:{p.Input.yaw} Key:{p.Input.key}]");
        return sb.ToString();
    }

}