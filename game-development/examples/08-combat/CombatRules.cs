using System.Text.Json;
using System.Text.Json.Serialization;

namespace Course.Combat;

/// <summary>One row of the weapon-versus-armor table: the multiplier applied when this weapon group hits this armor type.</summary>
public sealed record MatchupEntry(WeaponGroup Weapon, ArmorType Armor, double Multiplier);

/// <summary>
/// Every constant, table and weight the formulas use, as plain data. The defaults are one generic
/// sample ruleset; a different game supplies different numbers (for example from a JSON file, see
/// <see cref="FromJson"/>) without touching the formula or resolver code.
/// </summary>
public sealed record CombatRules
{
    // ---- Derived stats ----
    /// <summary>Upper limit of any single primary stat.</summary>
    public int MaxStat { get; init; } = 100;
    public int BaseHp { get; init; } = 50;
    public int HpPerVitality { get; init; } = 8;
    public int HpPerLevel { get; init; } = 10;
    /// <summary>HP regenerated per tick is <c>level * vitality / RegenDivisor</c>.</summary>
    public int RegenDivisor { get; init; } = 50;
    public int BaseMana { get; init; } = 20;
    public int ManaPerLevel { get; init; } = 4;
    public int ManaPerIntellect { get; init; } = 3;

    /// <summary>Rank = min(level, RankLevelCap) / RankLevelDivisor + gear level.</summary>
    public int RankLevelCap { get; init; } = 60;
    public int RankLevelDivisor { get; init; } = 3;

    public Dictionary<WeaponCategory, StatWeights> WeaponScaling { get; init; } = new()
    {
        [WeaponCategory.Melee] = new(Strength: 0.04, Agility: 0.01),
        [WeaponCategory.Ranged] = new(Strength: 0.01, Agility: 0.04),
        [WeaponCategory.Magic] = new(Intellect: 0.05),
    };

    /// <summary>Physical hit rate = HitBase + Agility * HitPerAgility (plus any bonus); magic skips this term.</summary>
    public double HitBase { get; init; } = 30;
    public double HitPerAgility { get; init; } = 0.5;

    public double MinAttackIntervalMs { get; init; } = 500;
    /// <summary>Each Agility point shortens the attack interval by this many percent.</summary>
    public double AgilitySpeedPercent { get; init; } = 0.5;

    /// <summary>Cast time in seconds = CastBaseSeconds + CastScaleSeconds / Intellect.</summary>
    public double CastBaseSeconds { get; init; } = 0.5;
    public double CastScaleSeconds { get; init; } = 10;

    // ---- Attack pipeline ----
    /// <summary>Each rank the attacker leads by lowers the defender's block and parry chance by this many points.</summary>
    public int AvoidPenaltyPerRank { get; init; } = 2;

    /// <summary>Defense removes <c>100 * def / (def + DefenseConstant + DefensePerRank * attackRank)</c> percent, capped.</summary>
    public double DefenseConstant { get; init; } = 100;
    public double DefensePerRank { get; init; } = 10;
    public double MaxDefenseReductionPercent { get; init; } = 75;

    /// <summary>
    /// Damage multiplier by rank gap, listed from "defender far ahead" to "attacker far ahead".
    /// The middle entry is a tie. The gap is clamped to the table's range: (length - 1) / 2 ranks.
    /// </summary>
    public double[] LevelGapCurve { get; init; } = [0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.1, 1.2, 1.3, 1.4, 1.5];

    public List<MatchupEntry> Matchups { get; init; } =
    [
        new(WeaponGroup.Pierce, ArmorType.Light, 1.25),
        new(WeaponGroup.Slash, ArmorType.Medium, 1.25),
        new(WeaponGroup.Blunt, ArmorType.Heavy, 1.25),
    ];

    public double CritBaseMultiplier { get; init; } = 2.0;
    public int MinDamage { get; init; } = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>Loads a ruleset from JSON. Properties that are left out keep the sample defaults.</summary>
    public static CombatRules FromJson(string json) =>
        JsonSerializer.Deserialize<CombatRules>(json, JsonOptions) ?? new CombatRules();
}
