namespace Course.Security;

public class MovementValidatorTests
{
    private const double Speed = 2.0;   // units per tick, from the server's own data

    private static MovementValidator NewValidator() 
    {
        var v = new MovementValidator(tolerance: 0.10);
        v.Spawn("p", new Point2(0, 0), tick: 0);
        return v;
    }

    [Fact]
    public void Check_HonestFullSpeedWalk_Accepted()
    {
        var v = NewValidator();
        var r = v.Check("p", new Point2(20, 0), tick: 10, Speed);   // 20 units in 10 ticks = exactly Speed

        Assert.True(r.Accepted);
        Assert.Equal(22.0, r.Allowed, precision: 6);
    }

    [Fact]
    public void Check_DoubleSpeedHack_RejectedAndSnappedBack()
    {
        var v = NewValidator();
        var r = v.Check("p", new Point2(40, 0), tick: 10, Speed);  // 40 units in 10 ticks

        Assert.False(r.Accepted);
        Assert.Equal(new Point2(0, 0), r.Position);
        Assert.Equal(40.0, r.Claimed, precision: 6);
        Assert.Equal(1, v.Violations("p"));
    }

    [Fact]
    public void Check_TeleportInOneTick_Rejected()
    {
        var v = NewValidator();
        var r = v.Check("p", new Point2(1000, 1000), tick: 1, Speed);
        Assert.False(r.Accepted);
    }

    [Fact]
    public void Check_SmallSustainedSpeedHack_StaysRejected()
    {
        var v = NewValidator();
        // 30% too fast, one packet per tick: 2.6 units per tick against an allowance of 2.2.
        for (var tick = 1; tick <= 50; tick++)
        {
            var r = v.Check("p", new Point2(2.6 * tick, 0), tick, Speed);
            Assert.False(r.Accepted);
        }
        Assert.Equal(50, v.Violations("p"));
    }

    [Fact]
    public void Check_TeleportSplitIntoSmallSteps_StillRejected()
    {
        var v = NewValidator();
        var accepted = 0;
        for (var tick = 1; tick <= 5; tick++)                         // claims to advance 10 units each tick
            if (v.Check("p", new Point2(10 * tick, 0), tick, Speed).Accepted) accepted++;
        Assert.Equal(0, accepted);
    }

    [Fact]
    public void Check_BunchedPacketsAfterLag_AcceptedBecauseElapsedTimeCounts()
    {
        var v = NewValidator();
        v.Check("p", new Point2(2, 0), tick: 1, Speed);
        // Nothing for 4 ticks, then a burst: the walker legitimately covered 8 more units.
        var r = v.Check("p", new Point2(10, 0), tick: 5, Speed);
        Assert.True(r.Accepted);
    }

    [Fact]
    public void Check_BuffRaisesTheLimit_OnlyBecauseTheServerSaysSo()
    {
        var v = NewValidator();
        Assert.False(v.Check("p", new Point2(30, 0), tick: 10, maxSpeedPerTick: 2.0).Accepted);
        Assert.True(v.Check("p", new Point2(30, 0), tick: 10, maxSpeedPerTick: 3.0).Accepted);   // server-side haste buff
    }
}
