namespace Course.GameLoop;

/// <summary>
/// The simulation's notion of time: a tick counter advanced one step at a time by
/// <see cref="ZoneLoop.Step"/>. Nothing in the rules core reads a wall clock
/// (no <c>DateTime.Now</c>, no <c>Environment.TickCount</c>) — "now" is always
/// "the current tick", so the same tick number always means the same instant,
/// regardless of how fast real time elapsed to get there.
/// </summary>
public sealed class GameClock
{
    /// <summary>How much simulated time one tick represents. Used only to translate
    /// real elapsed time into a tick count at the host boundary (see <see cref="LoopRunner"/>);
    /// the rules core itself only ever compares tick numbers.</summary>
    public TimeSpan TickDuration { get; }

    /// <summary>The tick number the simulation is currently on. Starts at 0.</summary>
    public long CurrentTick { get; private set; }

    public GameClock(TimeSpan tickDuration)
    {
        if (tickDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(tickDuration), "Tick duration must be positive.");

        TickDuration = tickDuration;
    }

    /// <summary>Advances the clock by exactly one tick.</summary>
    public void Advance() => CurrentTick++;
}
