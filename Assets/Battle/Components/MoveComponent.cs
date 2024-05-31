using System.IO;

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

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(common.position.x._raw);
        writer.Write(common.position.y._raw);
        writer.Write(common.position.z._raw);
        writer.Write(common.rotation.x._raw);
        writer.Write(common.rotation.y._raw);
        writer.Write(common.rotation.z._raw);
        writer.Write(common.rotation.w._raw);
        writer.Write(common.moveSpeed._raw);
        writer.Write(common.turnSpeed._raw);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.position = new FixedVector3(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        common.rotation = new FixedQuaternion(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        common.moveSpeed = new FixedNumber(reader.ReadInt64());
        common.turnSpeed = new FixedNumber(reader.ReadInt64());
    }
}