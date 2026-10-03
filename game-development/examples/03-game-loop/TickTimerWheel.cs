namespace Course.GameLoop;

/// <summary>
/// Tick-based timers: schedule a named timer to fire at an absolute tick, and ask
/// "what fires on tick N?" each step. This replaces the common wall-clock pattern for
/// cooldowns and respawns (an absolute wall-clock deadline
/// compared every frame, see the lesson, section 1.3) with an
/// absolute *tick* deadline compared once per <see cref="ZoneLoop.Step"/>. Same shape,
/// but now replayable and testable because "the current time" is a tick count, not a
/// call to the OS clock.
/// </summary>
public sealed class TickTimerWheel
{
    private readonly Dictionary<long, List<string>> _byTick = new();

    /// <summary>Schedules <paramref name="timerId"/> to fire at an absolute tick.</summary>
    public void ScheduleAt(long fireAtTick, string timerId)
    {
        if (!_byTick.TryGetValue(fireAtTick, out var ids))
        {
            ids = new List<string>();
            _byTick[fireAtTick] = ids;
        }

        ids.Add(timerId);
    }

    /// <summary>
    /// Returns (and removes) every timer id scheduled for exactly <paramref name="currentTick"/>.
    /// A timer that was scheduled for a tick the loop already passed (which cannot happen
    /// through <see cref="ZoneLoop"/>, since it always steps one tick at a time) would never fire —
    /// the same "a skipped frame can skip a cooldown" risk a wall-clock design has if an update
    /// is ever delayed past a deadline, which is why the host-side catch-up cap (<see cref="LoopRunner"/>)
    /// drops *frames*, never simulation ticks.
    /// </summary>
    public IReadOnlyList<string> Fire(long currentTick)
    {
        if (_byTick.Remove(currentTick, out var ids))
            return ids;

        return Array.Empty<string>();
    }
}
