namespace Course.Combat;

public class CombatFormulasTests
{
    private static readonly CombatFormulas F = new(new CombatRules());

    [Theory]
    [InlineData(30, 25, 0.5)]    // defender outranks the attacker by 5
    [InlineData(30, 30, 1.0)]
    [InlineData(25, 30, 1.5)]    // attacker outranks the defender by 5
    [InlineData(25, 1000, 1.5)]  // clamped at +5
    [InlineData(30, 29, 0.9)]
    public void GradeFix_RankPairs_MatchTheCurve(int defenseRank, int attackRank, double expected) =>
        Assert.Equal(expected, F.GradeFix(attackRank, defenseRank), 6);

    [Fact]
    public void EffectiveAvoid_Block40AttackerAhead5_Is30() =>
        Assert.Equal(30, F.EffectiveAvoid(40, attackRank: 30, defenseRank: 25));

    [Fact]
    public void EffectiveAvoid_AttackerBehind_IsUnchanged() =>
        Assert.Equal(40, F.EffectiveAvoid(40, attackRank: 25, defenseRank: 30));

    [Fact]
    public void DefensePercent_Def300Rank20_Is50() =>
        Assert.Equal(50.0, F.DefensePercent(300, 20), 6); // 100 * 300 / (300 + 100 + 200)

    [Fact]
    public void DefensePercent_HigherAttackerRank_ReducesTheSameDefense() =>
        Assert.True(F.DefensePercent(300, 40) < F.DefensePercent(300, 20));

    [Fact]
    public void DefensePercent_HugeDefense_IsCapped() =>
        Assert.Equal(75.0, F.DefensePercent(100_000, 20), 6);

    [Fact]
    public void ArmorTypeFix_ListedPair_UsesTheTableAndOthersAreNeutral()
    {
        Assert.Equal(1.25, F.ArmorTypeFix(WeaponGroup.Pierce, ArmorType.Light));
        Assert.Equal(1.0, F.ArmorTypeFix(WeaponGroup.Pierce, ArmorType.Heavy));
    }

    [Fact]
    public void CritMultiplier_Power50Defense20_Is2Point4() =>
        Assert.Equal(2.4, F.CritMultiplier(50, 20), 6); // 2.0 * 1.5 * 0.8
}
