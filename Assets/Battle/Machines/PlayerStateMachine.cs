public class PlayerStateMachine : BaseStateMachine<PlayerEntity>
{
    private static PlayerStateMachine _instance;

    public static PlayerStateMachine Instance
    {
        get
        {
            if (_instance == null)
                _instance = new PlayerStateMachine();
            return _instance;
        }
    }

    protected PlayerStateMachine() : base()
    {
        var types = GetType().Assembly.GetExportedTypes();
        stateDic = new BaseState<PlayerEntity>[(int)EPlayerState.Count];
        foreach (var t in types)
        {
            if (!t.IsDefined(typeof(PlayerState), false)) continue;
            
            var state = System.Activator.CreateInstance(t) as PlayerBaseState;
            var attributes = t.GetCustomAttributes(false);
            foreach (var t1 in attributes)
            {
                if (t1 is PlayerState)
                {
                    var attr = (t1 as PlayerState);
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
    
    public override void Update(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        var curState = stateDic[playerEntity.State.currStateId];
        curState.OnUpdate(playerEntity, battleEntity);
    }

    public override void LateUpdate(PlayerEntity playerEntity, BattleEntity battleEntity)
    {
        var curState = stateDic[playerEntity.State.currStateId];
        curState.OnLateUpdate(playerEntity, battleEntity);
    }

    public override bool DoChangeState(PlayerEntity entity, BattleEntity battleEntity)
    {
        var nextStateId = entity.State.nextStateId;
        if(nextStateId != 0 && stateDic[nextStateId] is PlayerBaseState nextState && nextState.TryEnter(entity, battleEntity))
        {
            var currState = stateDic[entity.State.currStateId] as PlayerBaseState;
            if (currState != null)
            {
                currState.OnExit(entity, battleEntity);
                entity.State.prevStateId = (int)currState.StateId;
            }
            nextState.Reset(entity, battleEntity);
            nextState.OnEnter(entity, battleEntity);
            if(entity.State.nextStateId != (int)EPlayerState.None)
            {
                return DoChangeState(entity, battleEntity);
            }
            return true;
        }
        else
        {
            entity.State.nextStateId = (int)EPlayerState.None;
        }
        return false;
    }
    
}