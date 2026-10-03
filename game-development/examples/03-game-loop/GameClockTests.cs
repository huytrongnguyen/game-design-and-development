namespace Course.GameLoop;

public class GameClockTests
{
    [Fact]
    public void Advance_CalledThreeTimes_CurrentTickIsThree()
    {
        var clock = new GameClock(TimeSpan.FromMilliseconds(100));

        clock.Advance();
        clock.Advance();
        clock.Advance();

        Assert.Equal(3, clock.CurrentTick);
    }

    [Fact]
    public void Constructor_NonPositiveTickDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new GameClock(TimeSpan.Zero));
    }
}
