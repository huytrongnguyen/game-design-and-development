namespace Course.GameLoop;

/// <summary>
/// The payoff of a deterministic, command-driven rules core: a recorded command log can
/// be replayed and must reproduce the exact same outcome. This is what makes "show me
/// exactly what happened in that bugged fight" (debugging) and headless balance
/// simulation possible at all.
/// </summary>
public class ReplayTests
{
    private const int Seed = 1234;

    // A fixed command log: deterministic deposits, a random dice roll, a timer, then
    // more deposits — applied identically to two independent simulations.
    private static List<ICommand> BuildCommandLog() => new()
    {
        new DepositCommand(100),
        new RollDiceCommand(Sides: 6),
        new ScheduleTimerCommand(DelayTicks: 2, TimerId: "evt-a"),
        new DepositCommand(25),
        new RollDiceCommand(Sides: 20),
    };

    private static ZoneState RunScenario(int seed, IReadOnlyList<ICommand> commandLog, int ticksToRun)
    {
        var clock = new GameClock(TimeSpan.FromMilliseconds(100));
        var queue = new CommandQueue();
        var state = new ZoneState();
        var loop = new ZoneLoop(clock, new SeededRng(seed), new TickTimerWheel(), queue, state);

        foreach (var command in commandLog)
        {
            queue.Submit(command);
        }

        for (int i = 0; i < ticksToRun; i++)
        {
            loop.Step();
        }

        return state;
    }

    [Fact]
    public void Replay_SameSeedAndCommandLog_ProducesIdenticalStateHash()
    {
        var firstRun = RunScenario(Seed, BuildCommandLog(), ticksToRun: 3);
        var secondRun = RunScenario(Seed, BuildCommandLog(), ticksToRun: 3);

        // The deterministic (non-random) parts match exactly, with concrete numbers: all
        // five commands were submitted before the first Step(), so they are all applied
        // on tick 0 -- deposits never depend on the RNG. 100 + 25 = 125.
        Assert.Equal(125, firstRun.Balance);
        Assert.Equal(125, secondRun.Balance);

        // The timer was scheduled with a 2-tick delay from tick 0, so it fires on tick 2 --
        // reached by the 3rd Step() call (ticksToRun: 3 processes ticks 0, 1, 2).
        Assert.Equal(new[] { "evt-a" }, firstRun.FiredTimers);
        Assert.Equal(new[] { "evt-a" }, secondRun.FiredTimers);

        // The random part is identical too: with seed 1234, System.Random on .NET 10
        // rolls a 6-sided die then a 20-sided die that sum to exactly 21 -- the same in
        // both runs, because both drew from a SeededRng(1234) in the same command order.
        Assert.Equal(21, firstRun.RollTotal);
        Assert.Equal(21, secondRun.RollTotal);

        // And therefore the whole-state replay hash matches too -- the actual property a
        // real replay/checksum system would check, instead of comparing every field.
        Assert.Equal(210712323449L, firstRun.ComputeHash());
        Assert.Equal(firstRun.ComputeHash(), secondRun.ComputeHash());
    }
}
