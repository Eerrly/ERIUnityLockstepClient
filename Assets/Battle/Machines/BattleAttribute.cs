public class PlayerState : System.Attribute
{
    public EPlayerState State;

    public PlayerState(EPlayerState state)
    {
        State = state;
    }
}

public class BattleState : System.Attribute
{
    public EBattleState State;

    public BattleState(EBattleState state)
    {
        State = state;
    }
}

/// <summary>
/// 实体系统
/// </summary>
public class EntitySystem : System.Attribute
{
    /// <summary>
    /// 初始化
    /// </summary>
    public class Initialize : System.Attribute { }

    /// <summary>
    /// 释放
    /// </summary>
    public class Release : System.Attribute { }
}