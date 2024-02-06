public class Entity
{
    public int Frame = -1;

    public int Data = -1;

    public void CopyTo(Entity entity)
    {
        entity.Frame = this.Frame;
        entity.Data = this.Data;
    }

    public void Reset()
    {
        this.Frame = -1;
        this.Data = -1;
    }

    public override string ToString()
    {
        return $"Frame: {Frame} Data: {Data}";
    }
}