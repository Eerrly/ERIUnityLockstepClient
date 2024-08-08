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

public class EntitySystem : System.Attribute
{
    public class Initialize : System.Attribute { }

    public class Release : System.Attribute { }
}