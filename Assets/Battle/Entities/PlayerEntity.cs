public class PlayerEntity : BaseEntity
{
    public int ID;

    public readonly InputComponent Input = new InputComponent();
    public readonly StateComponent State = new StateComponent();
    public readonly MoveComponent Movement = new MoveComponent();

    public override void Init()
    {
        ID = -1;
        
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;

        State.currStateId = (int)EPlayerState.None;
        State.prevStateId = (int)EPlayerState.None;
        State.nextStateId = (int)EPlayerState.Move;
        State.count = (int)EPlayerState.Count;

        Movement.moveSpeed = FixedNumber.MakeFixNum(5, 1);
        Movement.turnSpeed = FixedNumber.MakeFixNum(180, 1);
    }

    public override void Reset()
    {
        ID = -1;
        
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;
        
        State.currStateId = (int)EPlayerState.None;
        State.prevStateId = (int)EPlayerState.None;
        State.nextStateId = (int)EPlayerState.Move;
        State.count = (int)EPlayerState.Count;

        Movement.moveSpeed = FixedNumber.MakeFixNum(5, 1);
        Movement.turnSpeed = FixedNumber.MakeFixNum(180, 1);
    }

    public override void CopyTo(BaseEntity entity)
    {
        var playerEntity = entity as PlayerEntity;
        playerEntity.ID = ID;
        Input.CopyTo(playerEntity.Input);
        State.CopyTo(playerEntity.State);
        Movement.CopyTo(playerEntity.Movement);
    }

}