namespace Course.ActionCombat;

/// <summary>Server tuning for an action game, as data.</summary>
public sealed record ActionRules
{
    public int TickRate { get; init; } = 20;
    /// <summary>The server never rewinds further than this, so high-ping players cannot hit far into the past.</summary>
    public int MaxRewindTicks { get; init; } = 6;
    public int HistoryCapacity { get; init; } = 32;
    public double TickMs => 1000.0 / TickRate;
}

public static class LagCompensation
{
    /// <summary>Converts a duration to whole ticks, rounding up.</summary>
    public static int MsToTicks(double ms, ActionRules rules) => (int)Math.Ceiling(ms / rules.TickMs - 1e-9);

    /// <summary>
    /// How far back to rewind targets for a client: the one-way latency (the server's news took that long to
    /// arrive) plus the client's interpolation delay (it draws the world slightly in the past), clamped.
    /// </summary>
    public static int RewindTicks(double oneWayLatencyMs, double interpolationDelayMs, ActionRules rules) =>
        Math.Min(MsToTicks(oneWayLatencyMs + interpolationDelayMs, rules), rules.MaxRewindTicks);
}
