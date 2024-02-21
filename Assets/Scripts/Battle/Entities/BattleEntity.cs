using System.Collections.Generic;
using System.Linq;

public class BattleEntity : BaseEntity
{
    public int Frame = -1;

    public List<PlayerEntity> Players = new List<PlayerEntity>();

    public override void Init()
    {
        Frame = -1;
        Players = new List<PlayerEntity>();
    }

    public override void Reset()
    {
        Frame = -1;
        Players = new List<PlayerEntity>();
    }

    public PlayerEntity FindPlayerEntity(int playerEntityId)
    {
        return Players.FirstOrDefault(t => t.ID == playerEntityId);
    }

    public void CopyTo(BattleEntity entity)
    {
        entity.Frame = Frame;
        for (int i = 0; i < Players.Count; i++)
        {
            if(i >= entity.Players.Count) entity.Players.Add(new PlayerEntity());
            Players[i].CopyTo(entity.Players[i]);
        }
    }

}