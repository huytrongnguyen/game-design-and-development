namespace Course.Combat;

public class CombatRngTests
{
    [Fact]
    public void Next_SameSeed_SameSequence()
    {
        var a = new CombatRng(5);
        var b = new CombatRng(5);
        for (int i = 0; i < 50; i++)
            Assert.Equal(a.Next(1, 100), b.Next(1, 100));
    }

    [Fact]
    public void Next_Range1To100_StaysInsideAndCoversBothEnds()
    {
        var rng = new CombatRng(9);
        var seen = new HashSet<int>();
        for (int i = 0; i < 20_000; i++) seen.Add(rng.Next(1, 100));
        Assert.Equal(1, seen.Min());
        Assert.Equal(100, seen.Max());
    }
}
