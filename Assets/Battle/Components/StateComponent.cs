using System.IO;

public class StateComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public System.Int32 currStateId;
        public System.Int32 nextStateId;
        public System.Int32 prevStateId;
        public FixedNumber enteTime;
        public FixedNumber exitTime;
        public System.Int32 count;

        public Common(int no)
        {
            currStateId = default(System.Int32);
            nextStateId = default(System.Int32);
            prevStateId = default(System.Int32);
            enteTime = default(FixedNumber);
            exitTime = default(FixedNumber);
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

    public FixedNumber enteTime
    {
        get => common.enteTime;
        set => common.enteTime = value;
    }

    public FixedNumber exitTime
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
        writer.Write(common.enteTime._raw);
        writer.Write(common.exitTime._raw);
        writer.Write(common.count);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.currStateId = reader.ReadInt32();
        common.nextStateId = reader.ReadInt32();
        common.prevStateId = reader.ReadInt32();
        common.enteTime = new FixedNumber(reader.ReadInt64());
        common.exitTime = new FixedNumber(reader.ReadInt64());
        common.count = reader.ReadInt32();
    }
}