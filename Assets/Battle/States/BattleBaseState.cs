/// <summary>
/// 战斗状态基类
/// </summary>
public class BattleBaseState : BaseState<BattleEntity>
{
    /// <summary>
    /// 当前战斗状态
    /// </summary>
    public EBattleState StateId
    {
        get => (EBattleState)stateId;
        set => stateId = (int)value;
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <param name="_"></param>
    public override void Reset(BattleEntity entity, BattleEntity _)
    {
        entity.State.currStateId = (int)StateId;
        entity.State.nextStateId = 0;
    }

    /// <summary>
    /// 状态进入
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <param name="_"></param>
    public override void OnEnter(BattleEntity entity, BattleEntity _)
    {
        entity.State.enteTime = entity.Time;
    }

    /// <summary>
    /// 状态轮询
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="_"></param>
    public override void OnUpdate(BattleEntity entity, BattleEntity _)
    {
        entity.State.spanTime = entity.Time - entity.State.enteTime;
    }

    /// <summary>
    /// 状态退出
    /// </summary>
    /// <param name="entity">战斗实体</param>
    /// <param name="_"></param>
    public override void OnExit(BattleEntity entity, BattleEntity _)
    {
        entity.State.spanTime = entity.Time - entity.State.enteTime;
        entity.State.exitTime = entity.Time;
    }
    
}