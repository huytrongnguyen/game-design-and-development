namespace Course.Sync;

public class InterpolationBufferTests
{
    [Fact]
    public void TrySample_BetweenTwoSamples_BlendsLinearly()
    {
        var b = new InterpolationBuffer();
        b.Add(10, 0, 0);
        b.Add(14, 40, 20);

        Assert.True(b.TrySample(11, out var x, out var y));   // a quarter of the way

        Assert.Equal(10f, x);
        Assert.Equal(5f, y);
    }

    [Fact]
    public void TrySample_RenderingTwoTicksInThePast_UsesAlreadyReceivedData()
    {
        var b = new InterpolationBuffer();
        for (var t = 1; t <= 5; t++) b.Add(t, t * 10, 0);   // samples for ticks 1..5
        const double delay = 2.0;
        var now = 5.0;

        b.TrySample(now - delay, out var x, out _);

        Assert.Equal(30f, x);
    }

    [Fact]
    public void TrySample_PastNewestSample_HoldsInsteadOfExtrapolating()
    {
        var b = new InterpolationBuffer();
        b.Add(1, 0, 0);
        b.Add(2, 10, 0);

        b.TrySample(9, out var x, out _);

        Assert.Equal(10f, x);
    }

    [Fact]
    public void TrySample_Empty_ReturnsFalse()
    {
        Assert.False(new InterpolationBuffer().TrySample(1, out _, out _));
    }
}
