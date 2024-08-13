using System.Reflection;

/// <summary>
/// 实体基类
/// </summary>
public class BaseEntity
{
    /// <summary>
    /// 初始化
    /// </summary>
    public virtual void Init() {}

    /// <summary>
    /// 重置数据
    /// </summary>
    public virtual void Reset() {}

    /// <summary>
    /// 拷贝给另一个实体
    /// </summary>
    /// <param name="entity"></param>
    public virtual void CopyTo(BaseEntity entity) {}

    /// <summary>
    /// 序列化整个实体对象
    /// </summary>
    /// <param name="writer">写入流</param>
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

    /// <summary>
    /// 反序列化为整个实体对象
    /// </summary>
    /// <param name="reader">读取流</param>
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