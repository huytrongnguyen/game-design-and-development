using System.Text.Json;

namespace Course.Loot;

public sealed record Rarity(string Name, double Power, int Affixes);
public sealed record Slot(string Name, double Weight);

/// <summary>Stat budget of an item level: round(Base * (1 + GrowthPerLevel * (itemLevel - 1))).</summary>
public sealed record Budget(double Base, double GrowthPerLevel);

/// <summary>A drop source: the chance it drops gear at all, then rarity weights for that drop.</summary>
public sealed record Source(string Name, double DropChance, Dictionary<string, double> Weights);

/// <summary>Bad-luck protection: after SoftStartRuns misses the chance rises by SoftBonusPerRun each run; run HardAtRuns always drops.</summary>
public sealed record Pity(int SoftStartRuns, double SoftBonusPerRun, int HardAtRuns);

public sealed record Dungeon(string Name, int ItemLevel, int RollsPerRun, string Source, string TargetRarity, int Pieces, Pity Pity);

/// <summary>Step i takes an item from +(i-1) to +i. Failure from step DropFromStep on drops the item one level unless a charm is used.</summary>
public sealed record UpgradeStep(double Rate, double Gold);
public sealed record UpgradeRules(int MaxLevel, double PerLevelBonus, int DropFromStep, double CharmGold, UpgradeStep[] Steps);

/// <summary>Everything a designer edits, loaded from loot.json.</summary>
public sealed record LootData(Rarity[] Rarities, Slot[] Slots, Budget Budget, Source[] Sources, Dungeon Dungeon, UpgradeRules Upgrade)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static LootData FromJson(string json) => JsonSerializer.Deserialize<LootData>(json, Options)!;

    public static LootData Load(string? path = null) =>
        FromJson(File.ReadAllText(path ?? Path.Combine(AppContext.BaseDirectory, "loot.json")));

    public Rarity Rarity(string name) => Rarities.Single(r => r.Name == name);
    public Source Source(string name) => Sources.Single(s => s.Name == name);
}
