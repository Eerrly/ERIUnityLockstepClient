using System.Collections.Generic;

public class BattleEntityPool
{
    private readonly Queue<BattleEntity> _entities = new Queue<BattleEntity>();
    private readonly object _lock = new object();

    public void Enqueue(BattleEntity entity)
    {
        entity.Reset();
        lock (_lock) _entities.Enqueue(entity);
    }

    public BattleEntity Dequeue()
    {
        lock (_lock)
        {
            if(_entities.Count <= 0)
                _entities.Enqueue(new BattleEntity());
            return _entities.Dequeue();
        }
    }

}