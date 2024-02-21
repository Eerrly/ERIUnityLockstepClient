public class PlayerEntity : BaseEntity
{
    public int ID;

    #region Components

    public readonly InputComponent Input = new InputComponent();

    #endregion

    public override void Init()
    {
        ID = -1;
        Input.pos = 0;
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;
    }

    public override void Reset()
    {
        ID = -1;
        Input.pos = 0;
        Input.yaw = FixedMath.YawOffset;
        Input.key = 0;
    }

    public void CopyTo(PlayerEntity entity)
    {
        entity.ID = ID;
        Input.CopyTo(entity.Input);
    }

}