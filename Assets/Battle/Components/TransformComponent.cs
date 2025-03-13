using System.IO;

/// <summary>
/// 位置组件
/// </summary>
public class TransformComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public FixedVector3 pos;
        public FixedQuaternion rot;
        public FixedVector3 forward;

        public Common(int no)
        {
            pos = default(FixedVector3);
            rot = default(FixedQuaternion);
            forward = default(FixedVector3);
        }
    }
    
    private Common common = new Common(0);

    /// <summary>
    /// 当前位置
    /// </summary>
    public FixedVector3 pos
    {
        get => common.pos;
        set => common.pos = value;
    }

    /// <summary>
    /// 当前旋转
    /// </summary>
    public FixedQuaternion rot
    {
        get => common.rot;
        set => common.rot = value;
    }

    /// <summary>
    /// 当前朝向方向
    /// </summary>
    public FixedVector3 forward
    {
        get => common.forward;
        set => common.forward = value;
    }
    
    public override void CopyTo(BaseComponent component)
    {
        var data = component as TransformComponent;
        data.common = common;
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(common.pos.x._raw);
        writer.Write(common.pos.y._raw);
        writer.Write(common.pos.z._raw);
        writer.Write(common.rot.x._raw);
        writer.Write(common.rot.y._raw);
        writer.Write(common.rot.z._raw);
        writer.Write(common.rot.w._raw);
        writer.Write(common.forward.x._raw);
        writer.Write(common.forward.y._raw);
        writer.Write(common.forward.z._raw);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.pos = new FixedVector3(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        common.rot = new FixedQuaternion(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        common.forward = new FixedVector3(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
    }
}