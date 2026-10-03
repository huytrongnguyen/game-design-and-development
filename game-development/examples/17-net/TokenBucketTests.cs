namespace M17.Net;

public class TokenBucketTests
{
    [Fact]
    public void TryTake_BurstOfThree_FourthIsDenied()
    {
        long now = 0;
        var bucket = new TokenBucket(3, 1, () => now);
        Assert.True(bucket.TryTake());
        Assert.True(bucket.TryTake());
        Assert.True(bucket.TryTake());
        Assert.False(bucket.TryTake());
    }

    [Fact]
    public void TryTake_AfterOneSecondAtOnePerSecond_OneMoreIsAllowed()
    {
        long now = 0;
        var bucket = new TokenBucket(3, 1, () => now);
        for (var i = 0; i < 3; i++)
        {
            bucket.TryTake();
        }

        now = 1000;
        Assert.True(bucket.TryTake());
        Assert.False(bucket.TryTake());
    }

    [Fact]
    public void TryTake_LongIdle_RefillIsCappedAtCapacity()
    {
        long now = 0;
        var bucket = new TokenBucket(3, 1, () => now);
        now = 3_600_000;
        var allowed = 0;
        while (bucket.TryTake())
        {
            allowed++;
        }

        Assert.Equal(3, allowed);
    }
}
