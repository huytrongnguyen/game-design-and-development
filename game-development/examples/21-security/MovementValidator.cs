namespace Course.Security;

/// <summary>Server-side movement check: a claimed position may be at most
/// <c>maxSpeed x elapsed ticks x (1 + tolerance)</c> away from the last ACCEPTED position.
/// Measuring from the last accepted position (not the last claimed one) means a cheater cannot
/// pass by splitting a teleport into many small steps. The speed limit comes from the server's
/// own data (class, buffs, mount), never from the client.</summary>
public sealed class MovementValidator
{
    private sealed class Track { public Point2 Position; public long Tick; public int Violations; }

    private readonly double _tolerance;
    private readonly Dictionary<string, Track> _tracks = new();

    public MovementValidator(double tolerance = 0.10) => _tolerance = tolerance;

    public void Spawn(string entityId, Point2 position, long tick) =>
        _tracks[entityId] = new Track { Position = position, Tick = tick };

    public int Violations(string entityId) => _tracks[entityId].Violations;

    public MoveResult Check(string entityId, Point2 claimed, long tick, double maxSpeedPerTick)
    {
        var t = _tracks[entityId];
        var elapsed = Math.Max(1, tick - t.Tick);                        // jitter can bunch packets: never allow zero time
        var allowed = maxSpeedPerTick * elapsed * (1 + _tolerance);
        var distance = t.Position.DistanceTo(claimed);

        if (distance > allowed)
        {
            t.Violations++;                                              // feeds logging and detection
            return new MoveResult(false, t.Position, distance, allowed); // snap back to the authoritative position
        }

        t.Position = claimed;
        t.Tick = tick;
        return new MoveResult(true, claimed, distance, allowed);
    }
}
