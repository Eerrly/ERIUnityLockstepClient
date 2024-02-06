using System.Collections.Generic;

public class EntityPool
{
    private readonly Queue<Entity> _entities = new Queue<Entity>();
    private readonly object _lock = new object();

    public void Enqueue(Entity entity)
    {
        entity.Reset();
        lock (_lock) _entities.Enqueue(entity);
    }

    public Entity Dequeue()
    {
        lock (_lock)
        {
            if(_entities.Count <= 0)
                _entities.Enqueue(new Entity());
            return _entities.Dequeue();
        }
    }

}