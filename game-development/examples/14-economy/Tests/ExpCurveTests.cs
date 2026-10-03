namespace Course.Economy.Tests;

public class ExpCurveTests
{
    private static readonly ExpCurve Curve = new([100, 150, 225]);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(249, 2)]
    [InlineData(250, 3)]
    [InlineData(474, 3)]
    [InlineData(475, 4)]
    [InlineData(1_000_000, 4)] // capped
    public void LevelFor_ExactThresholds(long xp, int level) => Assert.Equal(level, Curve.LevelFor(xp));

    [Fact]
    public void TotalXpToReach_IsCumulative()
    {
        Assert.Equal(0, Curve.TotalXpToReach(1));
        Assert.Equal(250, Curve.TotalXpToReach(3));
        Assert.Equal(475, Curve.TotalXpToReach(4));
        Assert.Equal(150, Curve.XpToNext(2));
        Assert.Equal(0, Curve.XpToNext(4));
    }

    [Fact]
    public void Geometric_Ratio1500_BuildsTheSameTable()
    {
        var g = ExpCurve.Geometric(100, 1500, 3);
        Assert.Equal(475, g.TotalXpToReach(4));
        Assert.Equal(4, g.MaxLevel);
    }

    [Fact]
    public void FlatDecadeWall_OneLevelCostsMoreThanAllPreviousLevels()
    {
        // A level wall, scaled down: 9 smooth levels, then one step bigger than their sum.
        var steps = Enumerable.Repeat(10L, 9).Append(100L).ToArray();
        var curve = new ExpCurve(steps);
        Assert.True(curve.XpToNext(10) > curve.TotalXpToReach(10));
    }

    [Theory]
    [InlineData(400, 44, 50, 280)]  // 6 below: x0.70
    [InlineData(400, 60, 50, 480)]  // 10 above: x1.20
    [InlineData(400, 50, 50, 400)]  // same level: x1.00
    [InlineData(400, 30, 50, 120)]  // 20 below: grey x0.30
    [InlineData(400, 80, 50, 440)]  // 30 above: beyond the table x1.10
    public void KillXp_Gain_UsesTheLevelBand(int monXp, int monLv, int playerLv, long expected) =>
        Assert.Equal(expected, KillXp.Sample.Gain(monXp, monLv, playerLv));
}
