namespace Course.Combat;

/// <summary>The same code, a different game: only the data changes.</summary>
public class CombatRulesTests
{
    private const string AlternateRules = """
        {
          "maxStat": 255,
          "defenseConstant": 300,
          "critBaseMultiplier": 3.0,
          "matchups": [ { "weapon": "Blunt", "armor": "Light", "multiplier": 2.0 } ]
        }
        """;

    private static AttackerProfile Attacker() => new() { AttackRank = 20, Atk = 200, HitRate = 100 };
    private static DefenderProfile Defender() => new() { DefenseRank = 20, Def = 300 };

    [Fact]
    public void FromJson_OmittedProperties_KeepTheSampleDefaults()
    {
        var rules = CombatRules.FromJson(AlternateRules);
        Assert.Equal(255, rules.MaxStat);
        Assert.Equal(50, rules.BaseHp);
    }

    [Fact]
    public void FromJson_DifferentDefenseConstant_ChangesTheDamageWithoutCodeChanges()
    {
        var sample = new AttackResolver(new CombatRng(1), new CombatRules()).Resolve(Attacker(), Defender());
        var alternate = new AttackResolver(new CombatRng(1), CombatRules.FromJson(AlternateRules)).Resolve(Attacker(), Defender());

        Assert.Equal(100, sample.Damage);     // 300 / (300 + 100 + 200) = 50% removed
        Assert.Equal(125, alternate.Damage);  // 300 / (300 + 300 + 200) = 37.5% removed
    }

    [Fact]
    public void FromJson_ReplacedMatchupTable_ReplacesTheWholeTable()
    {
        var formulas = new CombatFormulas(CombatRules.FromJson(AlternateRules));
        Assert.Equal(2.0, formulas.ArmorTypeFix(WeaponGroup.Blunt, ArmorType.Light));
        Assert.Equal(1.0, formulas.ArmorTypeFix(WeaponGroup.Pierce, ArmorType.Light));
    }
}
