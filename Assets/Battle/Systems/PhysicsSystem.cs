/// <summary>
/// 物理系统
/// </summary>
[EntitySystem]
public static class PhysicsSystem
{
    /// <summary>
    /// 玩家们发生在物理计算更新前的一个位移向量
    /// </summary>
    private static FixedVector3[] _oldMoveDelta;
    
    /// <summary>
    /// 初始化
    /// </summary>
    [EntitySystem.Initialize]
    public static void Initialize()
    {
        _oldMoveDelta = new FixedVector3[BattleSetting.MaxPlayerInRoomCount];
    }

    /// <summary>
    /// 释放
    /// </summary>
    [EntitySystem.Release]
    public static void Release()
    {
        _oldMoveDelta = null;
    }
    
    /// <summary>
    /// 给各个玩家实体计算出其符合碰撞条件的玩家实体ID数组
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
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

                // 自己的碰撞半径+对方的碰撞半径
                var radius = source.Property.collisionSize + entity.Property.collisionSize;
                if ((entity.Transform.pos - source.Transform.pos).sqrMagnitudeLongXZ > radius * radius) continue;

                source.Property.closedPlayerEntityIds[entity.ID] = 1;
            }
        }
    }

    /// <summary>
    /// 通过计算来使发生了碰撞的玩家去修改其原有的位移方向，并且触发其当前状态的碰撞函数
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
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
                // 两个人位移方向的夹角大于90°
                if (FixedVector3.Dot(finalMoveDelta, direction) > FixedNumber.Zero)
                {
                    finalMoveDelta -= FixedVector3.Project(finalMoveDelta, direction);
                }
            }

            finalMoveDelta.y = source.Movement.position.y;
            source.Movement.position = finalMoveDelta;
        }
        // 触发其当前状态的碰撞函数
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