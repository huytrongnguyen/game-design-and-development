namespace Course.Sync;

/// <summary>
/// Server-side replication. The simulation mutates entities during a tick; <see cref="Replicate"/> runs
/// once at the end of the tick and turns "what changed" into one message list per observer, based on
/// what each observer already knows.
/// </summary>
public sealed class SyncServer
{
    private readonly SyncConfig _config;
    private readonly AoiGrid _grid;
    private readonly Dictionary<EntityId, Entity> _entities = new();
    // viewer -> (known entity -> tick of the last message sent about it)
    private readonly SortedDictionary<EntityId, Dictionary<EntityId, long>> _viewers = new();

    public SyncServer(SyncConfig config)
    {
        _config = config;
        _grid = new AoiGrid(config.CellSize);
    }

    /// <summary>The tick being simulated; changes made now are stamped with it.</summary>
    public long CurrentTick { get; private set; } = 1;

    public Entity Spawn(int id, float x, float y, int hp = 100, int appearance = 0)
    {
        var entity = new Entity(new EntityId(id), x, y, hp, appearance, CurrentTick);
        _entities.Add(entity.Id, entity);
        _grid.Insert(entity.Id, x, y);
        return entity;
    }

    public void Despawn(int id)
    {
        var key = new EntityId(id);
        _entities.Remove(key);
        _grid.Remove(key);
        _viewers.Remove(key);
    }

    /// <summary>Registers an entity as an observer (a player character). It sees others, not itself.</summary>
    public void AddViewer(int id) => _viewers.Add(new EntityId(id), new Dictionary<EntityId, long>());

    public void Move(int id, float x, float y)
    {
        var e = _entities[new EntityId(id)];
        e.SetPosition(x, y, CurrentTick);
        _grid.Move(e.Id, x, y);
    }

    public void SetHp(int id, int hp) => _entities[new EntityId(id)].SetHp(hp, CurrentTick);

    public int KnownCount(int viewerId) => _viewers[new EntityId(viewerId)].Count;

    /// <summary>End-of-tick pass: compute each observer's Leave, Enter and Update messages, then advance.</summary>
    public TickResult Replicate()
    {
        var tick = CurrentTick;
        var output = new Dictionary<EntityId, List<SyncMessage>>();
        var candidates = new List<EntityId>();
        var sight2 = _config.SightRange * _config.SightRange;
        var leave2 = _config.LeaveRange * _config.LeaveRange;
        var near2 = _config.NearRange * _config.NearRange;

        foreach (var (viewerId, known) in _viewers)
        {
            var messages = new List<SyncMessage>();
            output[viewerId] = messages;
            var me = _entities[viewerId];

            // 1. Leave: gone from the world, or beyond the (hysteresis) leave range. Never throttled.
            var gone = new List<EntityId>();
            foreach (var id in known.Keys)
                if (!_entities.TryGetValue(id, out var e) || Dist2(me, e) > leave2) gone.Add(id);
            gone.Sort();
            foreach (var id in gone)
            {
                known.Remove(id);
                messages.Add(new LeaveMessage(id));
            }

            // 2. Candidates from the grid, nearest first so the budget favours what matters most.
            candidates.Clear();
            _grid.Query(me.X, me.Y, _config.LeaveRange, candidates);
            candidates.Sort((a, b) =>
            {
                var c = Dist2(me, _entities[a]).CompareTo(Dist2(me, _entities[b]));
                return c != 0 ? c : a.CompareTo(b);
            });

            var bytes = 0;
            foreach (var id in candidates)
            {
                if (id == viewerId) continue;
                var e = _entities[id];
                var d2 = Dist2(me, e);

                SyncMessage message;
                if (known.TryGetValue(id, out var lastSent))
                {
                    var interval = d2 <= near2 ? 1 : _config.FarInterval;
                    if ((tick + id.Value) % interval != 0) continue;   // not this entity's turn
                    var mask = e.ChangedSince(lastSent);
                    if (mask == FieldMask.None) continue;               // nothing new: send nothing
                    message = new UpdateMessage(id, mask, e.X, e.Y, e.Hp, e.Appearance);
                }
                else
                {
                    if (d2 > sight2) continue;                           // inside leave ring but never seen
                    message = new EnterMessage(id, e.X, e.Y, e.Hp, e.Appearance);
                }

                var size = WireSize.Of(message);
                if (bytes + size > _config.BudgetBytesPerViewerTick) continue; // deferred, retried next tick
                bytes += size;
                known[id] = tick;
                messages.Add(message);
            }
        }

        CurrentTick++;
        return new TickResult(tick, output);
    }

    private static float Dist2(Entity a, Entity b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
