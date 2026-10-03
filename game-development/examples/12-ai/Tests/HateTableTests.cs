namespace M12.Ai.Tests;

public class HateTableTests
{
    [Fact]
    public void SelectTarget_ChallengerAt105Percent_KeepsCurrentTarget()
    {
        var h = new HateTable();
        h.AddDamage(1, 100);
        h.AddDamage(2, 105);
        Assert.Equal(1, h.SelectTarget(0, currentTargetId: 1));
    }

    [Fact]
    public void SelectTarget_ChallengerAt111Percent_SwitchesTarget()
    {
        var h = new HateTable();
        h.AddDamage(1, 100);
        h.AddDamage(2, 111);
        Assert.Equal(2, h.SelectTarget(0, currentTargetId: 1));
    }

    [Fact]
    public void AddHeal_Healing200_AddsThreat100()
    {
        var h = new HateTable();
        h.AddHeal(7, 200);
        Assert.Equal(100, h.ThreatOf(7));
    }

    [Fact]
    public void Decay_TenPercent_Reduces100To90AndDropsTinyEntries()
    {
        var h = new HateTable();
        h.Add(1, 100);
        h.Add(2, 1.05);
        h.Decay(0.1);
        Assert.Equal(90, h.ThreatOf(1), 6);
        Assert.Equal(1, h.Count);
    }

    [Fact]
    public void SelectTarget_TauntActive_ForcesTargetEvenWhenOthersHitHarder()
    {
        var h = new HateTable();
        h.AddDamage(1, 500);
        h.Taunt(2, currentTick: 10, durationTicks: 5);
        h.AddDamage(1, 1000);
        Assert.Equal(2, h.SelectTarget(14, 1));
        Assert.Equal(1, h.SelectTarget(15, 2)); // expired: 1500 vs 500
    }
}
