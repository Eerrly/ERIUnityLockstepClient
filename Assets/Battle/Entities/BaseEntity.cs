using System.Reflection;

public class BaseEntity
{
    public virtual void Init() {}

    public virtual void Reset() {}

    public virtual void CopyTo(BaseEntity entity) {}

    public virtual void Serialize(System.IO.BinaryWriter writer)
    {
        var componentFields = GetType().GetFields();
        foreach (var t in componentFields)
        {
            if(t.FieldType.BaseType != typeof(BaseComponent)) continue;
            var component = (BaseComponent)t.GetValue(this);
            component.Serialize(writer);
        }
    }

    public virtual void Deserialize(System.IO.BinaryReader reader)
    {
        var componentFields = GetType().GetFields();
        foreach (var t in componentFields)
        {
            if(t.FieldType.BaseType != typeof(BaseComponent)) continue;
            var component = (BaseComponent)t.GetValue(this);
            component.Deserialize(reader);
        }
    }
}