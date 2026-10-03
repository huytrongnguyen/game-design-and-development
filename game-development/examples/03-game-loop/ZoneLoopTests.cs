namespace Course.GameLoop;

public class ZoneLoopTests
{
    private static ZoneLoop NewLoop(int seed = 1234)
        => new(
            new GameClock(TimeSpan.FromMilliseconds(100)),
            new SeededRng(seed),
            new TickTimerWheel(),
            new CommandQueue(),
            new ZoneState());

    [Fact]
    public void Step_DepositCommandQueued_AddsAmountToBalance()
    {
        var queue = new CommandQueue();
        var state = new ZoneState();
        var loop = new ZoneLoop(new GameClock(TimeSpan.FromMilliseconds(100)), new SeededRng(1), new TickTimerWheel(), queue, state);

        queue.Submit(new DepositCommand(50));
        loop.Step();

        Assert.Equal(50, state.Balance);
    }

    [Fact]
    public void Step_CalledOnce_AdvancesCurrentTickByOne()
    {
        var loop = NewLoop();

        Assert.Equal(0, loop.CurrentTick);
        loop.Step();
        Assert.Equal(1, loop.CurrentTick);
    }

    [Fact]
    public void Step_TimerScheduledTwoTicksAhead_FiresOnlyOnTheExactTick()
    {
        var clock = new GameClock(TimeSpan.FromMilliseconds(100));
        var queue = new CommandQueue();
        var state = new ZoneState();
        var loop = new ZoneLoop(clock, new SeededRng(1), new TickTimerWheel(), queue, state);

        // Submitted while CurrentTick == 0, with a 3-tick delay: must fire exactly when
        // CurrentTick == 3, i.e. on the 4th call to Step() (which processes tick 0, then 1, 2, 3).
        queue.Submit(new ScheduleTimerCommand(DelayTicks: 3, TimerId: "boss-respawn"));

        var firedAtTick0 = loop.Step(); // processes tick 0: schedules the timer for tick 3
        var firedAtTick1 = loop.Step();
        var firedAtTick2 = loop.Step();
        var firedAtTick3 = loop.Step(); // processes tick 3: the timer is due
        var firedAtTick4 = loop.Step(); // already consumed: must not fire again

        Assert.Empty(firedAtTick0);
        Assert.Empty(firedAtTick1);
        Assert.Empty(firedAtTick2);
        Assert.Equal(new[] { "boss-respawn" }, firedAtTick3);
        Assert.Empty(firedAtTick4);
        Assert.Equal(new[] { "boss-respawn" }, state.FiredTimers);
    }

    [Fact]
    public void Step_CommandsSubmittedAfterDrainStarted_AreAppliedOnTheFollowingTick()
    {
        var clock = new GameClock(TimeSpan.FromMilliseconds(100));
        var queue = new CommandQueue();
        var state = new ZoneState();
        var loop = new ZoneLoop(clock, new SeededRng(1), new TickTimerWheel(), queue, state);

        queue.Submit(new DepositCommand(10));
        loop.Step(); // applies the 10 deposit on tick 0
        Assert.Equal(10, state.Balance);

        queue.Submit(new DepositCommand(5));
        // Not applied yet: Step() hasn't run since this Submit.
        Assert.Equal(10, state.Balance);

        loop.Step(); // applies the 5 deposit on tick 1
        Assert.Equal(15, state.Balance);
    }
}
