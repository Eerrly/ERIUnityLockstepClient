public class PlayerEntity : BaseEntity
{
    public int ID;

    public readonly InputComponent Input = new InputComponent();

    public override void Init()
    {
        ID = -1;
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;
    }

    public override void Reset()
    {
        ID = -1;
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;
    }

    public override void CopyTo(BaseEntity entity)
    {
        var playerEntity = entity as PlayerEntity;
        playerEntity.ID = ID;
        Input.CopyTo(playerEntity.Input);
    }

}