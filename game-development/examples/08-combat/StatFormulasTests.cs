namespace Course.Combat;

public class StatFormulasTests
{
    private static readonly StatFormulas F = new(new CombatRules());

    [Fact]
    public void MaxHp_Level20Vitality30_Is490() =>
        Assert.Equal(490, F.MaxHp(20, 30)); // 50 + 30*8 + 20*10

    [Fact]
    public void HpRegenPerTick_Level20Vitality30_Is12() =>
        Assert.Equal(12, F.HpRegenPerTick(20, 30)); // 20*30/50

    [Fact]
    public void MaxMana_Level20Intellect25_Is175() =>
        Assert.Equal(175, F.MaxMana(20, 25)); // 20 + 80 + 75

    [Fact]
    public void AttackRank_Level60Weapon10_Is30() =>
        Assert.Equal(30, F.AttackRank(60, 10)); // 60/3 + 10

    [Fact]
    public void AttackRank_LevelAboveTheCap_StopsGrowingFromLevel() =>
        Assert.Equal(F.AttackRank(60, 10), F.AttackRank(99, 10));

    [Fact]
    public void EffectiveStat_ClassBasePlusPoints_Is25() =>
        Assert.Equal(25, F.EffectiveStat(15, 10));

    [Fact]
    public void EffectiveStat_OverTheCap_ClampsTo100() =>
        Assert.Equal(100, F.EffectiveStat(95, 20));

    [Fact]
    public void Attack_MeleeStrength50Agility30_Is230()
    {
        var s = new PrimaryStats(Strength: 50, Agility: 30, Vitality: 0, Intellect: 0);
        Assert.Equal(2.3, F.WeaponScaling(s, WeaponCategory.Melee), 6); // 50*0.04 + 30*0.01
        Assert.Equal(230, F.Attack(100, s, WeaponCategory.Melee));
    }

    [Fact]
    public void WeaponScaling_MagicUsesOnlyIntellect()
    {
        var s = new PrimaryStats(Strength: 999, Agility: 999, Vitality: 0, Intellect: 40);
        Assert.Equal(2.0, F.WeaponScaling(s, WeaponCategory.Magic), 6);
    }

    [Fact]
    public void HitRate_PhysicalAgility40_Is50()
    {
        var s = new PrimaryStats(0, Agility: 40, 0, 0);
        Assert.Equal(50, F.HitRate(s, isMagic: false)); // 30 + 20
        Assert.Equal(0, F.HitRate(s, isMagic: true));
    }

    [Fact]
    public void AttackInterval_Base1000Agility40_Is800Ms() =>
        Assert.Equal(800, F.AttackIntervalMs(1000, 40), 2); // 1000 * (100 - 20) / 100

    [Fact]
    public void AttackInterval_VeryHighAgility_NeverBelowTheFloor() =>
        Assert.Equal(500, F.AttackIntervalMs(600, 100)); // would be 300

    [Fact]
    public void CastTime_Intellect20_Is1Second() =>
        Assert.Equal(1.0, F.CastTime(20), 6); // 0.5 + 10/20
}
