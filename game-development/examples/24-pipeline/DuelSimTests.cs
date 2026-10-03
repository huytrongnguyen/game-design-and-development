namespace M24.Pipeline;

public class DuelSimTests
{
    [Fact]
    public void Run_NoRandomnessAndStrongerFighter_WinsEveryDuel()
    {
        // A deals 20 per hit and needs 5 hits; B deals 10 and needs 10.
        var a = new Fighter("A", 100, 20, 0, 0);
        var b = new Fighter("B", 100, 10, 0, 0);

        var report = DuelSim.Run(a, b, 1000, baseSeed: 1);

        Assert.Equal(new DuelReport(1000, 1000, 0, 0), report);
    }

    [Fact]
    public void Run_SameSeed_GivesIdenticalReport()
    {
        var a = new Fighter("A", 100, 15, 3, 30);
        var b = new Fighter("B", 120, 12, 5, 20);

        Assert.Equal(DuelSim.Run(a, b, 1000, 42), DuelSim.Run(a, b, 1000, 42));
    }

    [Fact]
    public void Run_MirrorMatchWithRandomCrits_FirstStrikerHasAnEdgeButNotAHugeOne()
    {
        var f = new Fighter("F", 100, 15, 5, 50);

        var report = DuelSim.Run(f, f, 1000, 7);

        Assert.Equal(1000, report.WinsA + report.WinsB + report.Draws);
        Assert.InRange(report.WinRateA, 0.45, 0.70);
    }

    [Fact]
    public void Duel_FighterWhoCannotDamage_StillDealsAtLeastOne()
    {
        // Attack below defence: damage floors at 1, so A needs 10 hits against 10 HP.
        var a = new Fighter("A", 100, 1, 50, 0);
        var b = new Fighter("B", 10, 1, 50, 0);

        Assert.Equal(1, DuelSim.Duel(a, b, new Random(1)));
    }

    [Fact]
    public void Duel_NobodyCanKillWithinTheRoundCap_IsADraw()
    {
        var tank = new Fighter("T", 100000, 1, 0, 0);

        Assert.Equal(0, DuelSim.Duel(tank, tank, new Random(1)));
    }

    [Fact]
    public void Run_GoldenSeed_MatchesPinnedReport()
    {
        var a = new Fighter("A", 100, 15, 3, 30);
        var b = new Fighter("B", 120, 12, 5, 20);

        Assert.Equal(new DuelReport(1000, 770, 230, 0), DuelSim.Run(a, b, 1000, 42));
    }
}
