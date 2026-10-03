namespace Course.GameLoop;

/// <summary>
/// A single, discrete intent applied to the <see cref="ZoneState"/> on exactly one tick
/// (the Command pattern).
/// Commands are the only way the simulation changes: nothing else mutates
/// <see cref="ZoneState"/> directly, which is what makes a recorded command log replayable.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Applies this command's effect to <paramref name="state"/>. <paramref name="rng"/> and
    /// <paramref name="timers"/> are the only sources of non-determinism and scheduling a
    /// command may use; both are passed in rather than read from a static/global, so a command
    /// never reaches outside the arguments it is given.
    /// </summary>
    void Apply(ZoneState state, SeededRng rng, TickTimerWheel timers, long currentTick);
}
