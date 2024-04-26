using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleView : BaseView<BattleEntity>
{
    public string Name;
    private List<PlayerView> playerViews;

    public override void InitView(BattleEntity entity)
    {
        Name = entity.Name;
        playerViews = new List<PlayerView>();
        for (int i = 0; i < entity.PlayerEntities.Count; i++)
        {
            var playerEntity = entity.PlayerEntities[i];
            var playerView = Util.GetOrAddComponent<PlayerView>(new GameObject($"P-{playerEntity.ID}"));
            playerView.InitView(playerEntity);
            playerViews.Add(playerView);
        }
    }

    public override void RenderUpdate(BattleEntity entity, float deltaTime)
    {
        if(playerViews == null || playerViews.Count != entity.PlayerEntities.Count) return;
        
        foreach (var playerEntity in entity.PlayerEntities)
        {
            foreach (var t in playerViews.Where(t => t.ID == playerEntity.ID))
                t.RenderUpdate(playerEntity, deltaTime);
        }
    }

    public override void OnRelease(BattleEntity entity)
    {
        foreach (var playerView in playerViews)
            DestroyImmediate(playerView.gameObject);
        playerViews.Clear();
    }
}