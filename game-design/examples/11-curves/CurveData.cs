using System.Text.Json;

namespace Course.Curves;

/// <summary>XP needed to go from level L to L+1. Kind: "linear", "polynomial" or "exponential".</summary>
public sealed record XpCurve(string Kind, double Base, double Exponent = 0, double Slope = 0, double Growth = 1);

/// <summary>Kill XP of an on-level enemy is round(KillXpBase * L^KillXpExponent); a quest pays this many kills' worth.</summary>
public sealed record Income(double KillXpBase, double KillXpExponent, double QuestKillEquivalent);

/// <summary>A stretch of levels played in one region, with how fast a typical player earns XP there.</summary>
public sealed record Region(string Name, int FromLevel, int ToLevel, double KillsPerHour, double QuestsPerHour, double TargetHours);

public sealed record Milestone(string Name, int Level);

public sealed record Rested(double PoolPerOfflineHour, double PoolCapBars, double Bonus);

public sealed record GearAnchor(int Level, double Factor);

/// <summary>Hero power = level power x gear factor; content power = level power of the zone hub the hero is questing in.</summary>
public sealed record PowerPlan(double StatGrowth, int LevelsPerHub, double[] Band, GearAnchor[] Gear);

/// <summary>Everything a designer edits, loaded from curves.json.</summary>
public sealed record CurveData(
    int MaxLevel, double TotalTargetHours, double ToleranceHours, XpCurve Xp, Income Income,
    Region[] Regions, Milestone[] Milestones, Rested Rested, PowerPlan Power)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static CurveData FromJson(string json) => JsonSerializer.Deserialize<CurveData>(json, Options)!;

    public static CurveData Load(string? path = null) =>
        FromJson(File.ReadAllText(path ?? Path.Combine(AppContext.BaseDirectory, "curves.json")));

    public Region RegionOf(int level) => Regions.Single(r => level >= r.FromLevel && level <= r.ToLevel);
}
