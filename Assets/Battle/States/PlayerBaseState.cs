/// <summary>
/// 玩家状态基类
/// </summary>
[PlayerState(EPlayerState.None)]
public class PlayerBaseState : BaseState<PlayerEntity>
{
    /// <summary>
    /// 当前玩家状态
    /// </summary>
    public EPlayerState StateId
    {
        get => (EPlayerState)stateId;
        set => stateId = (int)value;
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public override void Reset(PlayerEntity entity, BattleEntity battleEntity)
    {
        entity.State.currStateId = (int)StateId;
        entity.State.nextStateId = 0;
    }

    /// <summary>
    /// 状态进入
    /// </summary>
    /// <param name="entity">玩家实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public override void OnEnter(PlayerEntity entity, BattleEntity battleEntity)
    {
        entity.State.enteTime = battleEntity.Time;
    }

    /// <summary>
    /// 状态退出
    /// </summary>
    /// <param name="entity">玩家状态</param>
    /// <param name="battleEntity">战斗状态</param>
    public override void OnExit(PlayerEntity entity, BattleEntity battleEntity)
    {
        entity.State.exitTime = battleEntity.Time;
    }

    /// <summary>
    /// 碰撞回调
    /// </summary>
    /// <param name="source">自己</param>
    /// <param name="target">他人</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void OnCollision(PlayerEntity source, PlayerEntity target, BattleEntity battleEntity)
    {
    }
    
}