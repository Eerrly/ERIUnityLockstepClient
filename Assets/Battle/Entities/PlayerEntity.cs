using System.IO;

/// <summary>
/// 玩家实体
/// </summary>
public class PlayerEntity : BaseEntity
{
    /// <summary>
    /// 玩家ID
    /// </summary>
    public int ID;

    /// <summary>
    /// 输入组件
    /// </summary>
    public readonly InputComponent Input = new InputComponent();
    /// <summary>
    /// 状态组件
    /// </summary>
    public readonly StateComponent State = new StateComponent();
    /// <summary>
    /// 位置组件
    /// </summary>
    public readonly TransformComponent Transform = new TransformComponent();
    /// <summary>
    /// 位移组件
    /// </summary>
    public readonly MoveComponent Movement = new MoveComponent();
    /// <summary>
    /// 属性组件
    /// </summary>
    public readonly PropertyComponent Property = new PropertyComponent();

    public override void Init()
    {
        ID = -1;
        
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;

        State.currStateId = (int)EPlayerState.None;
        State.prevStateId = (int)EPlayerState.None;
        State.nextStateId = (int)EPlayerState.Move;
        State.count = (int)EPlayerState.Count;
        
        Transform.pos = new FixedVector3(AreaSystem.Boundary.center.x, FixedNumber.One, AreaSystem.Boundary.center.y);
        Transform.rot = FixedQuaternion.Identity;

        Movement.moveSpeed = FixedNumber.MakeFixNum(5, 1);
        Movement.turnSpeed = FixedNumber.MakeFixNum(180, 1);
        
        Property.collisionSize = FixedNumber.MakeFixNum(5, 10);
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
        
        Property.collisionSize = FixedNumber.MakeFixNum(5, 10);
    }

    public override void CopyTo(BaseEntity entity)
    {
        var playerEntity = entity as PlayerEntity;
        playerEntity.ID = ID;
        Input.CopyTo(playerEntity.Input);
        State.CopyTo(playerEntity.State);
        Movement.CopyTo(playerEntity.Movement);
        Transform.CopyTo(playerEntity.Transform);
        Property.CopyTo(playerEntity.Property);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(ID);
        base.Serialize(writer);
    }

    public override void Deserialize(BinaryReader reader)
    {
        ID = reader.ReadInt32();
        base.Deserialize(reader);
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