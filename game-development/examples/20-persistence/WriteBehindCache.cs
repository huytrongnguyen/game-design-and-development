namespace Course.Persistence;

/// <summary>The hot copy of each online player's snapshot lives here. Gameplay updates it
/// freely; the database is written later, at checkpoints. Many updates become one write.</summary>
public sealed class WriteBehindCache
{
    private readonly IGameStore _store;
    private readonly CheckpointScheduler _scheduler;
    private readonly Dictionary<string, PlayerSnapshot> _live = new();

    public WriteBehindCache(IGameStore store, int intervalTicks)
    {
        _store = store;
        _scheduler = new CheckpointScheduler(intervalTicks);
    }

    public PlayerSnapshot Login(string playerId, long tick)
    {
        var snap = _store.LoadSnapshot(playerId) ?? new PlayerSnapshot(playerId, "start", 0, 0, 0);
        _live[playerId] = snap;
        _scheduler.Login(playerId, tick);
        return snap;
    }

    public PlayerSnapshot Get(string playerId) => _live[playerId];

    /// <summary>Memory only. Costs no database round trip.</summary>
    public void Update(PlayerSnapshot snapshot)
    {
        _live[snapshot.PlayerId] = snapshot;
        _scheduler.MarkDirty(snapshot.PlayerId);
    }

    public IReadOnlyList<Checkpoint> Tick(long tick)
    {
        var due = _scheduler.Tick(tick);
        foreach (var cp in due) Save(cp);
        return due;
    }

    public Checkpoint? ChangeZone(string playerId, string zone, long tick)
    {
        Update(_live[playerId] with { Zone = zone });
        return SaveIf(_scheduler.ZoneChanged(playerId, tick));
    }

    public Checkpoint? ImportantEvent(string playerId, long tick)
    {
        _scheduler.MarkDirty(playerId);
        return SaveIf(_scheduler.ImportantEvent(playerId, tick));
    }

    public Checkpoint? Logout(string playerId, long tick)
    {
        var cp = SaveIf(_scheduler.Logout(playerId, tick));
        _live.Remove(playerId);
        return cp;
    }

    private Checkpoint? SaveIf(Checkpoint? cp)
    {
        if (cp is { } c) Save(c);
        return cp;
    }

    private void Save(Checkpoint cp) => _store.SaveSnapshot(_live[cp.PlayerId]);
}
