namespace Course.Sync;

/// <summary>Applies Enter/Update/Leave messages to a local replica.</summary>
public sealed class ClientWorld
{
    private readonly Dictionary<EntityId, ClientEntity> _entities = new();

    public IReadOnlyDictionary<EntityId, ClientEntity> Entities => _entities;

    public void Apply(SyncMessage message, long tick)
    {
        switch (message)
        {
            case EnterMessage m:
                var e = new ClientEntity(m.Id) { X = m.X, Y = m.Y, Hp = m.Hp, Appearance = m.Appearance };
                e.Path.Add(tick, m.X, m.Y);
                _entities[m.Id] = e;
                break;
            case UpdateMessage m when _entities.TryGetValue(m.Id, out var u):
                if ((m.Mask & FieldMask.Position) != 0) { u.X = m.X; u.Y = m.Y; u.Path.Add(tick, m.X, m.Y); }
                if ((m.Mask & FieldMask.Hp) != 0) u.Hp = m.Hp;
                if ((m.Mask & FieldMask.Appearance) != 0) u.Appearance = m.Appearance;
                break;
            case LeaveMessage m:
                _entities.Remove(m.Id);
                break;
        }
    }

    public void ApplyAll(TickResult result, EntityId viewer)
    {
        foreach (var m in result.For(viewer)) Apply(m, result.Tick);
    }
}
