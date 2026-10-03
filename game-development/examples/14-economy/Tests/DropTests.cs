namespace Course.Economy.Tests;

public class DropTests
{
    private static int[] Counts(ulong seed, int rolls)
    {
        var table = new WeightedTable<int>([(0, 70), (1, 25), (2, 5)]);
        var rng = new SplitMixRng(seed);
        var counts = new int[3];
        for (var i = 0; i < rolls; i++) counts[table.Pick(rng)]++;
        return counts;
    }

    [Fact]
    public void Pick_10000SeededRolls_MatchWeightsWithinTolerance()
    {
        var c = Counts(42, 10_000);
        Assert.InRange(c[0], 6_800, 7_200); // 70% +/- 2 points
        Assert.InRange(c[1], 2_300, 2_700); // 25%
        Assert.InRange(c[2], 300, 700);     // 5%
        Assert.Equal(10_000, c.Sum());
    }

    [Fact]
    public void Pick_SameSeed_ReproducesExactly()
    {
        Assert.Equal(Counts(42, 10_000), Counts(42, 10_000));
        Assert.NotEqual(Counts(42, 10_000), Counts(43, 10_000));
    }

    [Fact]
    public void Roll_OneInFifty_10000SeededKillsDropAboutTwoHundred()
    {
        var table = new DropTable([new DropEntry("ore", 50)]);
        var rng = new SplitMixRng(7);
        var drops = 0;
        for (var i = 0; i < 10_000; i++) drops += table.Roll(rng).Count;
        Assert.InRange(drops, 140, 260); // expected 200, sigma about 14
    }

    [Fact]
    public void Roll_DenominatorOne_AlwaysDropsWithoutUsingTheRng()
    {
        var table = new DropTable([new DropEntry("potion", 1, 3)]);
        var rng = new SplitMixRng(1);
        var drops = table.Roll(rng);
        Assert.Equal(new DropResult("potion", 3), Assert.Single(drops));
        Assert.Equal(new SplitMixRng(1).NextUInt64(), rng.NextUInt64()); // rng untouched
    }

    [Theory]
    [InlineData(1000, 800, 1, 800)]    // near-level bonus: 1000 * 0.8
    [InlineData(1000, 3000, 1, 3000)]  // far below the killer: three times rarer
    [InlineData(1000, 1000, 2, 500)]   // double-drop event
    [InlineData(1, 3000, 1, 1)]        // guaranteed stays guaranteed
    public void Effective_ScalesTheDenominator(int denominator, int penalty, int rate, int expected) =>
        Assert.Equal(expected, new DropContext(penalty, rate).Effective(denominator));
}
