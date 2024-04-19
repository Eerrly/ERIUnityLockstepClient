public class PlayerStateAttribute : System.Attribute
{
    public EPlayerState _state;

    public PlayerStateAttribute(EPlayerState state)
    {
        _state = state;
    }
}

public class EntitySystemAttribute : System.Attribute
{
    public class Initialize : System.Attribute { }

    public class Release : System.Attribute { }
}