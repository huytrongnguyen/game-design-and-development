namespace Course.GameLoop;

public class TickTimerWheelTests
{
    [Fact]
    public void Fire_OnScheduledTick_ReturnsTimerId()
    {
        var timers = new TickTimerWheel();
        timers.ScheduleAt(fireAtTick: 5, timerId: "boss-respawn");

        var fired = timers.Fire(currentTick: 5);

        Assert.Equal(new[] { "boss-respawn" }, fired);
    }

    [Fact]
    public void Fire_OnDifferentTick_ReturnsEmpty()
    {
        var timers = new TickTimerWheel();
        timers.ScheduleAt(fireAtTick: 5, timerId: "boss-respawn");

        Assert.Empty(timers.Fire(currentTick: 4));
        Assert.Empty(timers.Fire(currentTick: 6));
    }

    [Fact]
    public void Fire_CalledTwiceForSameTick_FiresOnlyOnce()
    {
        var timers = new TickTimerWheel();
        timers.ScheduleAt(fireAtTick: 5, timerId: "boss-respawn");

        timers.Fire(currentTick: 5);
        var secondCall = timers.Fire(currentTick: 5);

        Assert.Empty(secondCall);
    }

    [Fact]
    public void ScheduleAt_TwoTimersOnSameTick_BothFireTogether()
    {
        var timers = new TickTimerWheel();
        timers.ScheduleAt(fireAtTick: 10, timerId: "wave-1");
        timers.ScheduleAt(fireAtTick: 10, timerId: "wave-2");

        var fired = timers.Fire(currentTick: 10);

        Assert.Equal(new[] { "wave-1", "wave-2" }, fired);
    }
}
