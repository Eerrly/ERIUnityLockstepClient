/// <summary>
/// 战斗实体状态控制
/// </summary>
public class BattleStateMachine : BaseStateMachine<BattleEntity>
{
    private static BattleStateMachine _instance;

    /// <summary>
    /// 单例
    /// </summary>
    public static BattleStateMachine Instance
    {
        get
        {
            if (_instance == null)
                _instance = new BattleStateMachine();
            return _instance;
        }
    }

    protected BattleStateMachine() : base()
    {
        var types = GetType().Assembly.GetExportedTypes();
        stateDic = new BaseState<BattleEntity>[(int)EPlayerState.Count];
        foreach (var t in types)
        {
            if (!t.IsDefined(typeof(BattleState), false)) continue;
            
            var state = System.Activator.CreateInstance(t) as BattleBaseState;
            var attributes = t.GetCustomAttributes(false);
            foreach (var t1 in attributes)
            {
                if (t1 is BattleState)
                {
                    var attr = (t1 as BattleState);
                    state.StateId = attr.State;
                }
            }

            var stateId = (int)state.StateId;

#if UNITY_EDITOR
            if (null != stateDic[stateId])
            {
                Logger.Log(LogLevel.Error, $"The {state.StateId} state has a instance, please check. now {t} other {stateDic[stateId].GetType()}");
            }
            else
#endif
            {
                stateDic[stateId] = state;
            }
#if UNITY_EDITOR
            var fields = t.GetFields();
            if (fields.Length > 0)
            {
                Logger.Log(LogLevel.Error,$"State:{t} has filed!");
            }

            var properties = t.GetProperties();
            if (properties.Length > 4)
            {
                Logger.Log(LogLevel.Error,$"State:{t} has property!");
            }
#endif
        }
    }

    /// <summary>
    /// 是否可以变更状态
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
    /// <returns>是否可以变更状态</returns>
    private bool CanChangeState(BattleEntity battleEntity)
    {
        if (battleEntity.State.nextStateId != (int)EBattleState.None && battleEntity.State.nextStateId != battleEntity.State.currStateId)
            return true;
        return false;
    }
    
    /// <summary>
    /// 状态控制轮询
    /// </summary>
    /// <param name="battleEntity">战斗实体</param>
    /// <param name="_"></param>
    public override void Update(BattleEntity battleEntity, BattleEntity _)
    {
        var currStateId = battleEntity.State.currStateId;
        var currState = stateDic[currStateId];
        if (CanChangeState(battleEntity) && stateDic[battleEntity.State.nextStateId].TryEnter(battleEntity, null))
        {
            if (currState != null)
            {
                battleEntity.State.prevStateId = currStateId;
                currState.OnExit(battleEntity, null);
            }
            currState = stateDic[battleEntity.State.nextStateId];
            currState.Reset(battleEntity, null);
            battleEntity.State.nextStateId = (int)EBattleState.None;
            currState.OnEnter(battleEntity, null);
        }
        if (currState != null)
        {
            currState.OnUpdate(battleEntity, null);
            currState.OnLateUpdate(battleEntity, null);
        }
    }

}