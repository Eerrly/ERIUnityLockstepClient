public class PlayerEntity : BaseEntity
{
    public int ID;

    public readonly InputComponent Input = new InputComponent();
    public readonly StateComponent State = new StateComponent();
    public readonly TransformComponent Transform = new TransformComponent();
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
        
        Transform.pos = FixedVector3.Zero;
        Transform.rot = FixedQuaternion.Identity;

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
        
        Transform.pos = FixedVector3.Zero;
        Transform.rot = FixedQuaternion.Identity;

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
        Transform.CopyTo(playerEntity.Transform);
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"| ID:{ID},");
        sb.Append($"Input->(Yaw:{Input.yaw} Key:{Input.key}),");
        sb.Append($"State->(CurrStateId:{State.currStateId} NextStateId:{State.nextStateId} PrevStateId:{State.prevStateId} EnteTime:{State.enteTime} ExitTime:{State.exitTime} Count:{State.count}),");
        sb.Append($"Transform->(Pos:{Transform.pos} Rot:{Transform.rot}),");
        sb.Append($"Movement->(Position:{Movement.position} Rotation:{Movement.rotation} MoveSpeed:{Movement.moveSpeed} TurnSpeed:{Movement.turnSpeed})");
        return sb.ToString();
    }

}