/// <summary>
/// 组件基类
/// </summary>
public class BaseComponent
{
    /// <summary>
    /// 拷贝给另一个组件
    /// </summary>
    /// <param name="component">组件</param>
    public virtual void CopyTo(BaseComponent component) {}
    
    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="writer">写入流</param>
    public virtual void Serialize(System.IO.BinaryWriter writer) {}
    
    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="reader">读取流</param>
    public virtual void Deserialize(System.IO.BinaryReader reader) {}
}