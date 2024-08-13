using System.IO;

/// <summary>
/// 位移组件
/// </summary>
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
        public FixedVector3 direction;

        public Common(int no)
        {
            position = default(FixedVector3);
            rotation = default(FixedQuaternion);
            curYAngle = default(FixedNumber);
            moveSpeed = default(FixedNumber);
            turnSpeed = default(FixedNumber);
            direction = default(FixedVector3);
        }
    }
    
    private Common common = new Common(0);

    /// <summary>
    /// 位移向量
    /// </summary>
    public FixedVector3 position
    {
        get => common.position;
        set => common.position = value;
    }

    /// <summary>
    /// 旋转四元数
    /// </summary>
    public FixedQuaternion rotation
    {
        get => common.rotation;
        set => common.rotation = value;
    }

    /// <summary>
    /// 当前角度
    /// </summary>
    public FixedNumber curYAngle
    {
        get => common.curYAngle;
        set => common.curYAngle = value;
    }

    /// <summary>
    /// 移动速度
    /// </summary>
    public FixedNumber moveSpeed
    {
        get => common.moveSpeed;
        set => common.moveSpeed = value;
    }

    /// <summary>
    /// 旋转速度
    /// </summary>
    public FixedNumber turnSpeed
    {
        get => common.turnSpeed;
        set => common.turnSpeed = value;
    }

    /// <summary>
    /// 移动方向
    /// </summary>
    public FixedVector3 direction
    {
        get => common.direction;
        set => common.direction = value;
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
        writer.Write(common.direction.x._raw);
        writer.Write(common.direction.y._raw);
        writer.Write(common.direction.z._raw);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.position = new FixedVector3(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        common.rotation = new FixedQuaternion(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        common.moveSpeed = new FixedNumber(reader.ReadInt64());
        common.turnSpeed = new FixedNumber(reader.ReadInt64());
        common.direction = new FixedVector3(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
    }
}