public class BaseStateMachine<T> where T : BaseEntity
{
    protected BaseState<T>[] stateDic;
    
    protected BaseStateMachine() { }

    public virtual BaseState<T> GetState(int stateId)
    {
        return stateDic[stateId];
    }
    
    public virtual void Update(T entity, BattleEntity battleEntity){ }
    
    public virtual void LateUpdate(T entity, BattleEntity battleEntity){ }

    public virtual bool DoChangeState(T entity, BattleEntity battleEntity)
    {
        return false;
    }
}