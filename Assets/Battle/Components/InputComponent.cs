using System.IO;

public class InputComponent: BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public System.Int32 yaw;
        public System.Int32 key;

        public Common(int no)
        {
            yaw = default(System.Int32);
            key = default(System.Int32);
        }
    }

    private Common common = new Common(0);

    public System.Int32 yaw 
    {
        get => common.yaw;
        set => common.yaw = value;
    }

    public System.Int32 key
    {
        get => common.key;
        set => common.key = value;
    }

    public override void CopyTo(BaseComponent component)
    {
        var data = component as InputComponent;
        data.common = common;
    }

    public override void Serialize(System.IO.BinaryWriter writer)
    {
        writer.Write(common.yaw);
        writer.Write(common.key);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.yaw = reader.ReadInt32();
        common.key = reader.ReadInt32();
    }
}