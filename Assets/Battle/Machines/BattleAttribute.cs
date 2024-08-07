public class PlayerState : System.Attribute
{
    public EPlayerState _state;

    public PlayerState(EPlayerState state)
    {
        _state = state;
    }
}

public class EntitySystem : System.Attribute
{
    public class Initialize : System.Attribute { }

    public class Release : System.Attribute { }
}