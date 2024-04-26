public class TransformComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public FixedVector3 pos;
        public FixedQuaternion rot;

        public Common(int no)
        {
            pos = default(FixedVector3);
            rot = default(FixedQuaternion);
        }
    }
    
    private Common common = new Common(0);

    public FixedVector3 pos
    {
        get => common.pos;
        set => common.pos = value;
    }

    public FixedQuaternion rot
    {
        get => common.rot;
        set => common.rot = value;
    }
    
    public override void CopyTo(BaseComponent component)
    {
        var data = component as TransformComponent;
        data.common = common;
    }
    
}