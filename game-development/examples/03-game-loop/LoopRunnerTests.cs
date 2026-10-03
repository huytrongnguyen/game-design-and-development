namespace Course.GameLoop;

public class LoopRunnerTests
{
    private static (LoopRunner Runner, ZoneLoop Loop) NewRunner(TimeSpan tickDuration, int maxTicksPerAdvance)
    {
        var clock = new GameClock(tickDuration);
        var loop = new ZoneLoop(clock, new SeededRng(1), new TickTimerWheel(), new CommandQueue(), new ZoneState());
        var runner = new LoopRunner(loop, tickDuration, maxTicksPerAdvance);
        return (runner, loop);
    }

    [Fact]
    public void Advance_ElapsedIsTwoAndAHalfTicks_RunsExactlyTwoTicksAndKeepsRemainder()
    {
        var (runner, loop) = NewRunner(TimeSpan.FromMilliseconds(100), maxTicksPerAdvance: 5);

        int ticksRun = runner.Advance(TimeSpan.FromMilliseconds(250));

        Assert.Equal(2, ticksRun);
        Assert.Equal(2, loop.CurrentTick);

        // The leftover 50ms is kept: one more 50ms brings the accumulator to the full
        // 100ms tick duration, so exactly one more tick should run.
        int secondTicksRun = runner.Advance(TimeSpan.FromMilliseconds(50));
        Assert.Equal(1, secondTicksRun);
        Assert.Equal(3, loop.CurrentTick);
    }

    [Fact]
    public void Advance_ElapsedOwesMoreTicksThanTheCap_RunsOnlyUpToTheCap()
    {
        var (runner, loop) = NewRunner(TimeSpan.FromMilliseconds(100), maxTicksPerAdvance: 5);

        // 10 real seconds at a 100ms tick would owe 100 ticks -- far past the cap of 5.
        int ticksRun = runner.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(5, ticksRun);
        Assert.Equal(5, loop.CurrentTick);
    }

    [Fact]
    public void Advance_AfterHittingTheCap_DropsTheSurplusInsteadOfCatchingUpLater()
    {
        var (runner, loop) = NewRunner(TimeSpan.FromMilliseconds(100), maxTicksPerAdvance: 5);

        runner.Advance(TimeSpan.FromSeconds(10)); // owed 100 ticks, capped at 5, 9.5s dropped
        Assert.Equal(5, loop.CurrentTick);

        // If the dropped 9.5s had been kept in the accumulator, this call would run
        // another 5 (capped) ticks. Because it was dropped, it runs zero.
        int ticksRunAfterDrop = runner.Advance(TimeSpan.Zero);

        Assert.Equal(0, ticksRunAfterDrop);
        Assert.Equal(5, loop.CurrentTick);
    }
}
