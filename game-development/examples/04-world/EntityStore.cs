namespace Course.World;

/// <summary>
/// Owns every entity and its components. An entity is only an <see cref="EntityId"/>; components are plain
/// structs in one table per type. Slots are recycled through a free list, and each reuse bumps the
/// slot's generation, so a stale handle is rejected instead of silently hitting a different entity.
/// </summary>
public sealed class EntityStore
{
    private readonly List<int> _generations = [];
    private readonly List<bool> _alive = [];
    private readonly Stack<int> _free = new();
    private readonly Dictionary<Type, object> _tables = [];
    private readonly List<Action<int>> _removers = [];

    public int AliveCount { get; private set; }

    public EntityId Spawn()
    {
        int index;
        if (_free.Count > 0)
        {
            index = _free.Pop();
            _alive[index] = true;
        }
        else
        {
            index = _generations.Count;
            _generations.Add(0);
            _alive.Add(true);
        }

        AliveCount++;
        return new EntityId(index, _generations[index]);
    }

    public bool IsAlive(EntityId id) =>
        id.Index >= 0 && id.Index < _generations.Count && _alive[id.Index] && _generations[id.Index] == id.Generation;

    /// <summary>Destroys the entity and all its components. Returns false for a stale or unknown handle.</summary>
    public bool Despawn(EntityId id)
    {
        if (!IsAlive(id))
            return false;

        foreach (var remove in _removers)
            remove(id.Index);

        _alive[id.Index] = false;
        _generations[id.Index]++;
        _free.Push(id.Index);
        AliveCount--;
        return true;
    }

    public bool Set<T>(EntityId id, T component) where T : struct
    {
        if (!IsAlive(id))
            return false;
        Table<T>().Set(id.Index, component);
        return true;
    }

    public bool TryGet<T>(EntityId id, out T component) where T : struct
    {
        component = default;
        return IsAlive(id) && Table<T>().TryGet(id.Index, out component);
    }

    public bool Has<T>(EntityId id) where T : struct => IsAlive(id) && Table<T>().Has(id.Index);

    private ComponentTable<T> Table<T>() where T : struct
    {
        if (_tables.TryGetValue(typeof(T), out var existing))
            return (ComponentTable<T>)existing;

        var table = new ComponentTable<T>();
        _tables[typeof(T)] = table;
        _removers.Add(table.Remove);
        return table;
    }
}
