/// <summary>
/// 状态控制
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public class BaseStateMachine<T> where T : BaseEntity
{
    /// <summary>
    /// 状态数组
    /// </summary>
    protected BaseState<T>[] stateDic;
    
    protected BaseStateMachine() { }

    /// <summary>
    /// 通过状态ID来获取状态
    /// </summary>
    /// <param name="stateId">状态ID</param>
    /// <returns>状态</returns>
    public virtual BaseState<T> GetState(int stateId)
    {
        return stateDic[stateId];
    }
    
    /// <summary>
    /// 状态Update
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void Update(T entity, BattleEntity battleEntity){ }
    
    /// <summary>
    /// 状态LateUpdate
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void LateUpdate(T entity, BattleEntity battleEntity){ }

    /// <summary>
    /// 变更状态
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    /// <returns></returns>
    public virtual bool DoChangeState(T entity, BattleEntity battleEntity)
    {
        return false;
    }
}