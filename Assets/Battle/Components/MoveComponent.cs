public class MoveComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public FixedVector3 position;
        public FixedQuaternion rotation;
        public FixedNumber curYAngle;
        public FixedNumber moveSpeed;
        public FixedNumber turnSpeed;

        public Common(int no)
        {
            position = default(FixedVector3);
            rotation = default(FixedQuaternion);
            curYAngle = default(FixedNumber);
            moveSpeed = default(FixedNumber);
            turnSpeed = default(FixedNumber);
        }
    }
    
    private Common common = new Common(0);

    public FixedVector3 position
    {
        get => common.position;
        set => common.position = value;
    }

    public FixedQuaternion rotation
    {
        get => common.rotation;
        set => common.rotation = value;
    }

    public FixedNumber curYAngle
    {
        get => common.curYAngle;
        set => common.curYAngle = value;
    }

    public FixedNumber moveSpeed
    {
        get => common.moveSpeed;
        set => common.moveSpeed = value;
    }

    public FixedNumber turnSpeed
    {
        get => common.turnSpeed;
        set => common.turnSpeed = value;
    }
    
    public override void CopyTo(BaseComponent component)
    {
        var data = component as MoveComponent;
        data.common = common;
    }
}