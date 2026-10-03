namespace Course.Combat;

/// <summary>Attack-pipeline formulas shared by every attack. All constants and tables come from <see cref="CombatRules"/>.</summary>
public sealed class CombatFormulas(CombatRules rules)
{
    /// <summary>Level-gap multiplier from the rules' curve: low when the defender outranks the attacker, high when outranked.</summary>
    public double GradeFix(int attackRank, int defenseRank)
    {
        var curve = rules.LevelGapCurve;
        int reach = (curve.Length - 1) / 2;
        int diff = Math.Clamp(defenseRank - attackRank, -reach, reach);
        return curve[reach - diff];
    }

    /// <summary>The attacker's rank advantage erodes the defender's block/parry chance.</summary>
    public int EffectiveAvoid(int avoidStat, int attackRank, int defenseRank) =>
        avoidStat - Math.Max(0, (attackRank - defenseRank) * rules.AvoidPenaltyPerRank);

    /// <summary>Percent of damage removed by defense. Higher attacker rank makes the same defense worth less.</summary>
    public double DefensePercent(int def, int attackRank)
    {
        double denominator = def + rules.DefenseConstant + rules.DefensePerRank * attackRank;
        double percent = denominator <= 0 ? 0 : 100.0 * def / denominator;
        return Math.Clamp(percent, 0, rules.MaxDefenseReductionPercent);
    }

    /// <summary>Weapon-versus-armor multiplier from the matchup table; pairs that are not listed deal 1.0x.</summary>
    public double ArmorTypeFix(WeaponGroup weapon, ArmorType armor)
    {
        foreach (var m in rules.Matchups)
            if (m.Weapon == weapon && m.Armor == armor) return m.Multiplier;
        return 1.0;
    }

    /// <summary>Crit multiplier: the base multiplier, scaled by the attacker's crit power and the defender's crit defense.</summary>
    public double CritMultiplier(int critPower, int critDefense) =>
        rules.CritBaseMultiplier * (100 + critPower) / 100 * (100 - critDefense) / 100;
}
