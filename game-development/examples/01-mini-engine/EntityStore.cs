namespace Course.MiniEngine;

/// <summary>
/// The entity/component store. An entity is nothing but an <see cref="EntityId"/>; components are plain
/// structs attached to it, one dictionary per component type. Deliberately simple (boxes each component
/// into <see cref="object"/>, no archetypes/sparse sets) so the mechanism stays readable — see module 01
/// §5's "what this example leaves out" for what a production store would add.
/// </summary>
public sealed class EntityStore
{
    private readonly List<EntityId> _creationOrder = [];
    private readonly Dictionary<Type, Dictionary<EntityId, object>> _components = [];
    private int _nextId;

    public EntityId CreateEntity()
    {
        var id = new EntityId(_nextId++);
        _creationOrder.Add(id);
        return id;
    }

    public void Set<T>(EntityId id, T component) where T : struct => ComponentMap<T>()[id] = component;

    public bool TryGet<T>(EntityId id, out T component) where T : struct
    {
        if (_components.TryGetValue(typeof(T), out var map) && map.TryGetValue(id, out var boxed))
        {
            component = (T)boxed;
            return true;
        }

        component = default;
        return false;
    }

    public bool Has<T>(EntityId id) where T : struct =>
        _components.TryGetValue(typeof(T), out var map) && map.ContainsKey(id);

    public void Remove<T>(EntityId id) where T : struct
    {
        if (_components.TryGetValue(typeof(T), out var map))
            map.Remove(id);
    }

    /// <summary>Entities that currently have a <typeparamref name="T"/> component, in creation order.</summary>
    public IEnumerable<EntityId> EntitiesWith<T>() where T : struct
    {
        if (!_components.TryGetValue(typeof(T), out var map))
            yield break;

        foreach (var id in _creationOrder)
            if (map.ContainsKey(id))
                yield return id;
    }

    private Dictionary<EntityId, object> ComponentMap<T>() where T : struct
    {
        if (!_components.TryGetValue(typeof(T), out var map))
        {
            map = [];
            _components[typeof(T)] = map;
        }

        return map;
    }
}
