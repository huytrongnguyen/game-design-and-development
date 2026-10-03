namespace Course.Persistence;

/// <summary>Decides WHEN a player's snapshot must be written. Time is the tick number, so the
/// behaviour is deterministic. Clean players never cost a write.</summary>
public sealed class CheckpointScheduler
{
    private sealed class Clock { public long LastSaved; public bool Dirty; }

    private readonly int _interval;
    private readonly Dictionary<string, Clock> _online = new();

    public CheckpointScheduler(int intervalTicks) => _interval = intervalTicks;

    public void Login(string playerId, long tick) =>
        _online[playerId] = new Clock { LastSaved = tick };

    public void MarkDirty(string playerId)
    {
        if (_online.TryGetValue(playerId, out var c)) c.Dirty = true;
    }

    /// <summary>Interval checkpoints due at this tick, in player-id order.</summary>
    public IReadOnlyList<Checkpoint> Tick(long tick)
    {
        var due = new List<Checkpoint>();
        foreach (var (id, c) in _online.OrderBy(kv => kv.Key, StringComparer.Ordinal))
        {
            if (!c.Dirty || tick - c.LastSaved < _interval) continue;
            c.LastSaved = tick; c.Dirty = false;
            due.Add(new Checkpoint(id, tick, CheckpointReason.Interval));
        }
        return due;
    }

    /// <summary>Always checkpoints: the position just changed zone.</summary>
    public Checkpoint? ZoneChanged(string playerId, long tick) => Now(playerId, tick, CheckpointReason.ZoneChange, force: true);

    /// <summary>Always checkpoints: a level-up, a rare drop, a quest reward.</summary>
    public Checkpoint? ImportantEvent(string playerId, long tick) => Now(playerId, tick, CheckpointReason.ImportantEvent, force: true);

    /// <summary>Checkpoints only if something is unsaved, then forgets the player.</summary>
    public Checkpoint? Logout(string playerId, long tick)
    {
        var cp = Now(playerId, tick, CheckpointReason.Logout, force: false);
        _online.Remove(playerId);
        return cp;
    }

    private Checkpoint? Now(string playerId, long tick, CheckpointReason reason, bool force)
    {
        if (!_online.TryGetValue(playerId, out var c)) return null;
        if (!force && !c.Dirty) return null;
        c.LastSaved = tick; c.Dirty = false;
        return new Checkpoint(playerId, tick, reason);
    }
}
