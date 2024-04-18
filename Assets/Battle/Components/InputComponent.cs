public class InputComponent: BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public System.Int32 pos;
        public System.Int32 yaw;
        public System.Int32 key;

        public Common(int no)
        {
            pos = default(System.Int32);
            yaw = default(System.Int32);
            key = default(System.Int32);
        }
    }

    private Common common = new Common(0);

    public System.Int32 pos 
    {
        get => common.pos;
        set => common.pos = value;
    }

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
}