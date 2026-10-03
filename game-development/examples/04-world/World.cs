namespace Course.World;

/// <summary>
/// The world: one entity store, many zones, and a log of membership events. All lifecycle
/// operations go through here so the store and the zone grids can never disagree.
/// </summary>
public sealed class World
{
    private readonly Dictionary<ZoneId, Zone> _zones = [];
    private readonly Dictionary<EntityId, ZoneId> _zoneOf = [];
    private readonly List<WorldEvent> _events = [];

    public EntityStore Store { get; } = new();

    public Zone AddZone(ZoneId id, float width, float height, float cellSize)
    {
        var zone = new Zone(id, Store, width, height, cellSize);
        _zones.Add(id, zone);
        return zone;
    }

    public Zone? GetZone(ZoneId id) => _zones.GetValueOrDefault(id);

    public ZoneId? ZoneOf(EntityId id) => _zoneOf.TryGetValue(id, out var z) ? z : null;

    /// <summary>Creates an entity with the standard components and enters it into a zone. Returns null if the zone or position is invalid.</summary>
    public EntityId? Spawn(ZoneId zoneId, string name, Position at, int maxHealth)
    {
        if (!_zones.TryGetValue(zoneId, out var zone) || !zone.InBounds(at))
            return null;

        var id = Store.Spawn();
        Store.Set(id, new Name(name));
        Store.Set(id, at);
        Store.Set(id, new Health(maxHealth, maxHealth));
        Enter(zone, id);
        return id;
    }

    /// <summary>Removes the entity from its zone (emitting Left) and destroys it. False for a stale handle.</summary>
    public bool Despawn(EntityId id)
    {
        if (!Store.IsAlive(id))
            return false;

        if (_zoneOf.TryGetValue(id, out var zoneId))
            Leave(_zones[zoneId], id);

        return Store.Despawn(id);
    }

    /// <summary>Moves within the current zone. False if stale, not in a zone, or the target is out of bounds.</summary>
    public bool Move(EntityId id, Position to)
    {
        if (!Store.IsAlive(id) || !_zoneOf.TryGetValue(id, out var zoneId))
            return false;

        var zone = _zones[zoneId];
        if (!zone.InBounds(to))
            return false;

        Store.Set(id, to);
        zone.Update(id);
        return true;
    }

    /// <summary>
    /// Moves the entity to another zone: validates first, then emits Left (old zone) followed by Entered (new zone).
    /// Nothing changes if any check fails.
    /// </summary>
    public bool Transfer(EntityId id, ZoneId target, Position at)
    {
        if (!Store.IsAlive(id) || !_zoneOf.TryGetValue(id, out var current) || current == target)
            return false;
        if (!_zones.TryGetValue(target, out var to) || !to.InBounds(at))
            return false;

        Leave(_zones[current], id);
        Store.Set(id, at);
        Enter(to, id);
        return true;
    }

    /// <summary>Returns the events since the last call, oldest first, and clears the log.</summary>
    public List<WorldEvent> DrainEvents()
    {
        var copy = new List<WorldEvent>(_events);
        _events.Clear();
        return copy;
    }

    private void Enter(Zone zone, EntityId id)
    {
        zone.Add(id);
        _zoneOf[id] = zone.Id;
        _events.Add(new WorldEvent(WorldEventKind.Entered, id, zone.Id));
    }

    private void Leave(Zone zone, EntityId id)
    {
        zone.Remove(id);
        _zoneOf.Remove(id);
        _events.Add(new WorldEvent(WorldEventKind.Left, id, zone.Id));
    }
}
