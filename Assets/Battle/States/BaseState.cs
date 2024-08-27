/// <summary>
/// 状态基类
/// </summary>
/// <typeparam name="T"></typeparam>
public class BaseState<T> where T : BaseEntity
{
    /// <summary>
    /// 当前状态ID
    /// </summary>
    protected System.Int32 stateId;

    /// <summary>
    /// 重置
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void Reset(T entity, BattleEntity battleEntity)
    {
    }

    /// <summary>
    /// 尝试进入状态
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    /// <returns>是否可以进入状态</returns>
    public virtual bool TryEnter(T entity, BattleEntity battleEntity)
    {
        return true;
    }

    /// <summary>
    /// 状态进入
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void OnEnter(T entity, BattleEntity battleEntity)
    {
    }

    /// <summary>
    /// 状态Update
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void OnUpdate(T entity, BattleEntity battleEntity)
    {
    }

    /// <summary>
    /// 状态LateUpdate
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void OnLateUpdate(T entity, BattleEntity battleEntity)
    {
    }

    /// <summary>
    /// 状态退出
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="battleEntity">战斗实体</param>
    public virtual void OnExit(T entity, BattleEntity battleEntity)
    {
    }

}