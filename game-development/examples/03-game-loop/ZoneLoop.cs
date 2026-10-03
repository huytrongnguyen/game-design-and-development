namespace Course.GameLoop;

/// <summary>
/// The rules core of a zone server loop (see the lesson): one fixed-size step, always in the same order, with no wall-clock reads
/// and no I/O. A host (<see cref="LoopRunner"/>, a test, a console harness) decides *when*
/// to call <see cref="Step"/>; this type only decides *what happens* on a step.
/// </summary>
/// <remarks>
/// Per tick, in order:
/// <list type="number">
/// <item>Drain every command submitted since the last tick, ordered deterministically
/// (<see cref="CommandQueue.DrainOrdered"/>). Entities then act on their queued
/// requests once per tick, in a fixed order.</item>
/// <item>Apply each command to <see cref="State"/>, in that order.</item>
/// <item>Fire whatever timers are due on the current tick (<see cref="TickTimerWheel.Fire"/>).</item>
/// <item>Advance the clock by exactly one tick.</item>
/// </list>
/// There is no accumulator and no "catch-up" inside this type: one call always advances
/// the simulation by exactly one tick. Catching up after a slow frame is the host's job
/// (<see cref="LoopRunner.Advance"/>), kept deliberately out of the rules core so the core
/// stays trivial to unit test (call <see cref="Step"/> N times, assert exact state).
/// </remarks>
public sealed class ZoneLoop
{
    private readonly GameClock _clock;
    private readonly SeededRng _rng;
    private readonly TickTimerWheel _timers;
    private readonly CommandQueue _commands;

    public ZoneState State { get; }

    public long CurrentTick => _clock.CurrentTick;

    public ZoneLoop(GameClock clock, SeededRng rng, TickTimerWheel timers, CommandQueue commands, ZoneState state)
    {
        _clock = clock;
        _rng = rng;
        _timers = timers;
        _commands = commands;
        State = state;
    }

    /// <summary>Advances the simulation by exactly one tick. Returns the ids of any timers that fired this tick.</summary>
    public IReadOnlyList<string> Step()
    {
        long tick = _clock.CurrentTick;

        var commands = _commands.DrainOrdered();
        foreach (var command in commands)
        {
            command.Apply(State, _rng, _timers, tick);
        }

        var fired = _timers.Fire(tick);
        foreach (var timerId in fired)
        {
            State.FiredTimers.Add(timerId);
        }

        _clock.Advance();

        return fired;
    }
}
