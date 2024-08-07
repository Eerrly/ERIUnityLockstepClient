[EntitySystem]
public static class PhysicsSystem
{
    private static FixedVector3[] _oldMoveDelta;
    
    [EntitySystem.Initialize]
    public static void Initialize()
    {
        _oldMoveDelta = new FixedVector3[BattleSetting.MaxPlayerInRoomCount];
    }

    [EntitySystem.Release]
    public static void Release()
    {
        _oldMoveDelta = null;
    }
    
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
                if ((entity.Transform.pos - source.Transform.pos).sqrMagnitudeLongXZ > radius * radius) continue;

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
            _oldMoveDelta[source.ID] = source.Movement.position;
        }
        for (var i = 0; i < playerEntities.Count; i++)
        {
            var source = playerEntities[i];
            var closedPlayerEntityIds = source.Property.closedPlayerEntityIds;
            var finalMoveDelta = source.Movement.position.YZero();
            for (var entityId = 0; entityId < closedPlayerEntityIds.Length; entityId++)
            {
                if(closedPlayerEntityIds[entityId] == 0) continue;

                var entity = battleEntity.FindPlayerEntity(entityId);
                var direction = (entity.Transform.pos - source.Transform.pos).YZero();
                if (FixedVector3.Dot(finalMoveDelta, direction) > FixedNumber.Zero)
                {
                    finalMoveDelta -= FixedVector3.Project(finalMoveDelta, direction);
                }
            }

            finalMoveDelta.y = source.Movement.position.y;
            source.Movement.position = finalMoveDelta;
        }
        for (var i = 0; i < playerEntities.Count; i++)
        {
            var source = playerEntities[i];
            var closedPlayerEntityIds = source.Property.closedPlayerEntityIds;
            for (int entityId = 0; entityId < closedPlayerEntityIds.Length; entityId++)
            {
                if(closedPlayerEntityIds[entityId] == 0) continue;
                
                var entity = battleEntity.FindPlayerEntity(entityId);
                var moveDelta = _oldMoveDelta[entity.ID];
                var direction = (entity.Transform.pos - source.Transform.pos).YZero();
                if (FixedVector3.Dot(direction, moveDelta) <= FixedNumber.Zero) continue;
                
                (PlayerStateMachine.Instance.GetState(source.State.currStateId) as PlayerBaseState)?.OnCollision(source, entity, battleEntity);
                (PlayerStateMachine.Instance.GetState(entity.State.currStateId) as PlayerBaseState)?.OnCollision(entity, source, battleEntity);
            }
        }
    }
    
}