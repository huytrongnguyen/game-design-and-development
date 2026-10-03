using System.Diagnostics;

namespace Course.GameLoop;

/// <summary>
/// The host-side bridge between real/wall-clock time and the fixed-tick rules core —
/// the one place in this example allowed to read a real clock, because it is
/// infrastructure, not simulation (the "fixed timestep" pattern from module 03).
/// In production this would be the body of a hosted background service; here it is a plain class so
/// <see cref="Advance"/> — the part with actual logic — can be unit tested without any
/// hosting, waiting, or real time elapsing.
/// </summary>
public sealed class LoopRunner
{
    /// <summary>
    /// How many ticks a single call to <see cref="Advance"/> will ever run. If more ticks
    /// than this are owed (e.g. after a debugger pause, a GC pause, or the process being
    /// suspended), the surplus is dropped rather than simulated all at once — the standard
    /// fixed-timestep "spiral of death" guard (Fiedler, "Fix Your Timestep!"): without a cap,
    /// a long stall makes the next <c>Advance</c> call take even longer (to run all the owed
    /// ticks), which falls further behind, which owes even more ticks next time.
    /// </summary>
    public const int DefaultMaxTicksPerAdvance = 5;

    private readonly ZoneLoop _loop;
    private readonly TimeSpan _tickDuration;
    private readonly int _maxTicksPerAdvance;
    private TimeSpan _accumulator;

    public LoopRunner(ZoneLoop loop, TimeSpan tickDuration, int maxTicksPerAdvance = DefaultMaxTicksPerAdvance)
    {
        if (maxTicksPerAdvance < 1)
            throw new ArgumentOutOfRangeException(nameof(maxTicksPerAdvance));

        _loop = loop;
        _tickDuration = tickDuration;
        _maxTicksPerAdvance = maxTicksPerAdvance;
    }

    /// <summary>
    /// Given that <paramref name="elapsed"/> real time has passed since the last call,
    /// runs as many fixed ticks as are now owed, capped at <see cref="DefaultMaxTicksPerAdvance"/>
    /// (or the constructor override). Pure with respect to its input — it never reads a
    /// real clock itself — which is what makes it directly unit-testable
    /// (see <c>LoopRunnerTests</c>): feed it a <see cref="TimeSpan"/>, assert how many
    /// ticks ran.
    /// </summary>
    /// <returns>The number of ticks actually run (0..<see cref="DefaultMaxTicksPerAdvance"/>).</returns>
    public int Advance(TimeSpan elapsed)
    {
        _accumulator += elapsed;

        int ticksRun = 0;
        while (_accumulator >= _tickDuration && ticksRun < _maxTicksPerAdvance)
        {
            _loop.Step();
            _accumulator -= _tickDuration;
            ticksRun++;
        }

        if (ticksRun == _maxTicksPerAdvance && _accumulator >= _tickDuration)
        {
            // Still behind after running the cap: drop the surplus instead of letting it
            // compound into the next call. We fall permanently behind real time rather than
            // spiral; a production host would also log this as a performance warning.
            _accumulator = TimeSpan.Zero;
        }

        return ticksRun;
    }

    /// <summary>
    /// Runs the loop for real, on a fixed cadence, until <paramref name="ct"/> is cancelled —
    /// the shape of a production hosted service's main method. Not exercised by tests:
    /// it depends on real elapsed time and an async wait, which <see cref="Advance"/> was
    /// specifically factored out to avoid needing.
    /// </summary>
    public async Task RunAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(_tickDuration);
        var stopwatch = Stopwatch.StartNew();
        var last = stopwatch.Elapsed;

        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
        {
            var now = stopwatch.Elapsed;
            Advance(now - last);
            last = now;
        }
    }
}
