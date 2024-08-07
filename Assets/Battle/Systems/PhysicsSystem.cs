[EntitySystem]
public static class PhysicsSystem
{
    public static void PreUpdate(BattleEntity battleEntity)
    {
        var playerEntities = battleEntity.PlayerEntities;
        for (var i = 0; i < playerEntities.Count; i++)
        {
            var source = playerEntities[i];
            var closedPlayerEntityIds = source.Property.closedPlayerEntityIds;
            System.Array.Clear(closedPlayerEntityIds, 0, closedPlayerEntityIds.Length);
            for (var j = 0; j < playerEntities.Count; j++)
            {
                var entity = playerEntities[j];
                if (source.ID == entity.ID) continue;

                var radius = source.Property.collisionSize + entity.Property.collisionSize;
                if ((entity.Transform.pos - source.Transform.pos).sqrMagnitudeLongXZ >= radius * radius) continue;

                source.Property.closedPlayerEntityIds[entity.ID] = 1;
            }
        }
    }

    public static void Update(BattleEntity battleEntity)
    {
        var playerEntities = battleEntity.PlayerEntities;
        for (var i = 0; i < playerEntities.Count; i++)
        {
            var source = playerEntities[i];
            var closedPlayerEntityIds = source.Property.closedPlayerEntityIds;
            var finalMoveDelta = source.Movement.position.YZero();
            for (var entityId = 0; entityId < closedPlayerEntityIds.Length; entityId++)
            {
                if(closedPlayerEntityIds[entityId] == 0) continue;

                var entity = battleEntity.FindPlayerEntity(entityId);
                var vec = (entity.Transform.pos - source.Transform.pos).YZero();
                if (FixedVector3.Dot(finalMoveDelta, vec) > FixedNumber.Zero)
                {
                    finalMoveDelta -= FixedVector3.Project(finalMoveDelta, vec);
                }
            }

            finalMoveDelta.y = source.Movement.position.y;
            source.Movement.position = finalMoveDelta;
        }
    }
    
}