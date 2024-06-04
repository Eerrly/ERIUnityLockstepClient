using System.IO;

public class StateComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public System.Int32 currStateId;
        public System.Int32 nextStateId;
        public System.Int32 prevStateId;
        public float enteTime;
        public float exitTime;
        public System.Int32 count;

        public Common(int no)
        {
            currStateId = default(System.Int32);
            nextStateId = default(System.Int32);
            prevStateId = default(System.Int32);
            enteTime = default(float);
            exitTime = default(float);
            count = default(System.Int32);
        }
    }
    
    private Common common = new Common(0);

    public System.Int32 currStateId
    {
        get => common.currStateId;
        set => common.currStateId = value;
    }

    public System.Int32 nextStateId
    {
        get => common.nextStateId;
        set => common.nextStateId = value;
    }

    public System.Int32 prevStateId
    {
        get => common.prevStateId;
        set => common.prevStateId = value;
    }

    public float enteTime
    {
        get => common.enteTime;
        set => common.enteTime = value;
    }

    public float exitTime
    {
        get => common.exitTime;
        set => common.exitTime = value;
    }

    public System.Int32 count
    {
        get => common.count;
        set => common.count = value;
    }
    
    public override void CopyTo(BaseComponent component)
    {
        var data = component as StateComponent;
        data.common = common;
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write(common.currStateId);
        writer.Write(common.nextStateId);
        writer.Write(common.prevStateId);
        writer.Write(common.enteTime);
        writer.Write(common.exitTime);
        writer.Write(common.count);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.currStateId = reader.ReadInt32();
        common.nextStateId = reader.ReadInt32();
        common.prevStateId = reader.ReadInt32();
        common.enteTime = reader.ReadSingle();
        common.exitTime = reader.ReadSingle();
        common.count = reader.ReadInt32();
    }
}