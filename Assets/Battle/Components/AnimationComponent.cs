using System.IO;

public class AnimationComponent : BaseComponent
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Auto, Pack = 4)]
    internal struct Common
    {
        public EAnimationID animId;
        public System.Int32 index;
        public FixedNumber startTime;
        // public FixedVector3 position;
        // public FixedQuaternion rotation;

        public Common(int no)
        {
            animId = default(EAnimationID);
            index = default(System.Int32);
            startTime = default(FixedNumber);
            // position = default(FixedVector3);
            // rotation = default(FixedQuaternion);
        }
    }
    
    private Common common = new Common(0);

    /// <summary>
    /// 动画ID
    /// </summary>
    public EAnimationID animId
    {
        get => common.animId;
        set => common.animId = value;
    }

    /// <summary>
    /// 事件索引
    /// </summary>
    public System.Int32 index
    {
        get => common.index;
        set => common.index = value;
    }

    /// <summary>
    /// 动画开始时间
    /// </summary>
    public FixedNumber startTime
    {
        get => common.startTime;
        set => common.startTime = value;
    }

    // /// <summary>
    // /// 动画位移
    // /// </summary>
    // public FixedVector3 position
    // {
    //     get => common.position;
    //     set => common.position = value;
    // }
    //
    // /// <summary>
    // /// 动画旋转
    // /// </summary>
    // public FixedQuaternion rotation
    // {
    //     get => common.rotation;
    //     set => common.rotation = value;
    // }
    

    private System.Collections.Generic.List<EAnimationEvent> _currentEvents = new System.Collections.Generic.List<EAnimationEvent>();
    /// <summary>
    /// 动画事件集合
    /// </summary>
    public System.Collections.Generic.List<EAnimationEvent> currentEvents
    {
        get => _currentEvents;
        set => _currentEvents = value;
    }

    public override void CopyTo(BaseComponent component)
    {
        var data = component as AnimationComponent;
        data.common = common;
        data._currentEvents.Clear();
        data._currentEvents.Capacity = data._currentEvents.Capacity > _currentEvents.Capacity ? data._currentEvents.Capacity : _currentEvents.Capacity;
        data._currentEvents.AddRange(_currentEvents);
    }

    public override void Serialize(BinaryWriter writer)
    {
        writer.Write((int)common.animId);
        writer.Write(common.index);
        writer.Write(common.startTime._raw);
        // writer.Write(common.position.x._raw);
        // writer.Write(common.position.y._raw);
        // writer.Write(common.position.z._raw);
        // writer.Write(common.rotation.x._raw);
        // writer.Write(common.rotation.y._raw);
        // writer.Write(common.rotation.z._raw);
        // writer.Write(common.rotation.w._raw);
        writer.Write(_currentEvents.Count);
        for (int i = 0; i < _currentEvents.Count; i++)
            writer.Write((int)_currentEvents[i]);
    }

    public override void Deserialize(BinaryReader reader)
    {
        common.animId = (EAnimationID)reader.ReadInt32();
        common.index = reader.ReadInt32();
        common.startTime = new FixedNumber(reader.ReadInt64());
        // common.position = new FixedVector3(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        // common.rotation = new FixedQuaternion(new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()), new FixedNumber(reader.ReadInt64()));
        var _currentEvents_Count = reader.ReadInt32();
        _currentEvents.Clear();
        for (int i = 0; i < _currentEvents_Count; i++)
        {
            _currentEvents.Add(default(EAnimationEvent));
            _currentEvents[i] = (EAnimationEvent)reader.ReadInt32();
        }
    }
}