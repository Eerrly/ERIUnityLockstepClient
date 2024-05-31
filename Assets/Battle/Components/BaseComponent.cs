public class BaseComponent
{
    public virtual void CopyTo(BaseComponent component) {}
    
    public virtual void Serialize(System.IO.BinaryWriter writer) {}
    
    public virtual void Deserialize(System.IO.BinaryReader reader) {}
}