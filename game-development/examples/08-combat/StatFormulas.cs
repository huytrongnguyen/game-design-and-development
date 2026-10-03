namespace Course.Combat;

/// <summary>Character stat formulas. Pure functions of their inputs and the rules: same inputs, same output, no state.</summary>
public sealed class StatFormulas(CombatRules rules)
{
    /// <summary>Class base plus freely allocated points, capped per stat.</summary>
    public int EffectiveStat(int classBase, int allocatedPoints) => Math.Min(rules.MaxStat, classBase + allocatedPoints);

    public int MaxHp(int level, int vitality) =>
        (int)Math.Max(1, Math.Floor((double)rules.BaseHp + vitality * rules.HpPerVitality + level * rules.HpPerLevel));

    public int HpRegenPerTick(int level, int vitality) => (int)Math.Floor(level * vitality / (double)rules.RegenDivisor);

    public int MaxMana(int level, int intellect) =>
        rules.BaseMana + level * rules.ManaPerLevel + intellect * rules.ManaPerIntellect;

    private int LevelPart(int level) => Math.Min(level, rules.RankLevelCap) / rules.RankLevelDivisor;

    /// <summary>Attack rank: the attacker's "power level" for rank-gap checks.</summary>
    public int AttackRank(int level, int weaponLevel) => LevelPart(level) + weaponLevel;

    /// <summary>Defense rank: same shape, built from the armor's level.</summary>
    public int DefenseRank(int level, int armorLevel) => LevelPart(level) + armorLevel;

    /// <summary>Which primaries scale weapon damage depends on the weapon category (weights come from the rules).</summary>
    public double WeaponScaling(PrimaryStats s, WeaponCategory category) =>
        rules.WeaponScaling.TryGetValue(category, out var w) ? w.Apply(s) : 0;

    public int Attack(int weaponAtk, PrimaryStats s, WeaponCategory category, double multiplier = 1.0) =>
        (int)Math.Max(0, Math.Floor(Math.Round(weaponAtk * WeaponScaling(s, category) * multiplier, 6)));

    /// <summary>Physical hit rate; magic skips the Agility term.</summary>
    public int HitRate(PrimaryStats s, bool isMagic, int bonus = 0)
    {
        double v = bonus;
        if (!isMagic) v += rules.HitBase + s.Agility * rules.HitPerAgility;
        return (int)Math.Clamp(v, 0, 100);
    }

    /// <summary>Attack interval in milliseconds (lower is faster), with a floor.</summary>
    public double AttackIntervalMs(double baseMs, int agility) =>
        Math.Max(rules.MinAttackIntervalMs, baseMs * (100 - agility * rules.AgilitySpeedPercent) / 100);

    /// <summary>Cast time in seconds; higher Intellect is faster.</summary>
    public double CastTime(int intellect) => rules.CastBaseSeconds + rules.CastScaleSeconds / intellect;
}
