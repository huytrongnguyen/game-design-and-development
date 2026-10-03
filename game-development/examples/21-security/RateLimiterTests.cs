namespace Course.Security;

public class RateLimiterTests
{
    [Fact]
    public void Allow_FloodOf100InOneTick_OnlyTheBurstPasses()
    {
        var rl = new SessionRateLimiter(capacity: 5, refillPerTick: 0.1, maxStrikes: 1000);

        var verdicts = Enumerable.Range(0, 100).Select(_ => rl.Allow("s", tick: 0)).ToList();

        Assert.Equal(5, verdicts.Count(v => v == RateVerdict.Allowed));
        Assert.Equal(95, verdicts.Count(v => v == RateVerdict.Throttled));
    }

    [Fact]
    public void Allow_AfterWaiting_TokensRefillUpToTheCapacity()
    {
        var rl = new SessionRateLimiter(5, 0.1, 1000);
        for (var i = 0; i < 5; i++) rl.Allow("s", 0);

        var after30 = Enumerable.Range(0, 10).Select(_ => rl.Allow("s", 30)).ToList();   // 30 ticks x 0.1 = 3 tokens
        Assert.Equal(3, after30.Count(v => v == RateVerdict.Allowed));

        var after1000 = Enumerable.Range(0, 10).Select(_ => rl.Allow("s", 1000)).ToList(); // refill is capped at 5
        Assert.Equal(5, after1000.Count(v => v == RateVerdict.Allowed));
    }

    [Fact]
    public void Allow_SteadyHonestRate_NeverThrottled()
    {
        var rl = new SessionRateLimiter(5, 0.1, 3);
        for (var tick = 0; tick < 1000; tick += 10)   // one message every 10 ticks = exactly the refill rate
            Assert.Equal(RateVerdict.Allowed, rl.Allow("s", tick));
    }

    [Fact]
    public void Allow_SustainedFlood_EventuallyDisconnects()
    {
        var rl = new SessionRateLimiter(5, 0.1, maxStrikes: 20);
        var verdicts = Enumerable.Range(0, 100).Select(_ => rl.Allow("s", 0)).ToList();

        Assert.Equal(RateVerdict.Disconnect, verdicts[5 + 19]);   // 5 allowed, then the 20th consecutive refusal
    }

    [Fact]
    public void Allow_SessionsAreIndependent()
    {
        var rl = new SessionRateLimiter(1, 0.0, 1000);
        Assert.Equal(RateVerdict.Allowed, rl.Allow("a", 0));
        Assert.Equal(RateVerdict.Throttled, rl.Allow("a", 0));
        Assert.Equal(RateVerdict.Allowed, rl.Allow("b", 0));
    }
}
