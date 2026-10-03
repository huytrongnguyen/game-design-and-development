namespace Course.GameLoop;

/// <summary>
/// Schedules a named timer to fire <see cref="DelayTicks"/> ticks after the tick this
/// command is applied on. This is the tick-based alternative to storing an absolute
/// wall-clock deadline (see the lesson, section 1.3).
/// </summary>
public sealed record ScheduleTimerCommand(long DelayTicks, string TimerId) : ICommand
{
    public void Apply(ZoneState state, SeededRng rng, TickTimerWheel timers, long currentTick)
        => timers.ScheduleAt(currentTick + DelayTicks, TimerId);
}
