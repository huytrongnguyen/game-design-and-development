using System.Text.Json;

namespace Course.Balance;

/// <summary>How much each benchmark fight cares about damage, staying alive and utility. Should sum to 1.</summary>
public sealed record Weights(double Dps, double Survivability, double Utility);

/// <summary>A benchmark fight: how many enemies stand in the area, and how armoured they are.</summary>
public sealed record Fight(string Name, double Targets, double EnemyDefence, Weights Weights);

/// <summary>A Path (level 20 choice) as multipliers on the base class kit.</summary>
public sealed record PathMod(string Name, double DamageMult, double AreaMult, double EhpMult, double UtilityDelta);

/// <summary>Module 08's combat stats, plus what a balance sheet needs on top: mitigation (damage removed by the kit:
/// blocks, shields, self-heals), a 0-10 utility score, and how much of the damage is area (share and max targets).</summary>
public sealed record HeroClass(
    string Name, string Role, double Hp, double Defence, double DamagePerHit, double AttackInterval,
    double CritChance, double CritMultiplier, double Mitigation, double Utility,
    double AreaShare, int MaxTargets, PathMod[] Paths);

/// <summary>Target bands: class score [min, max] against the roster mean (1.0), and the largest allowed Path gap.</summary>
public sealed record Bands(double[] ClassScore, double PathGap);

/// <summary>Everything a designer edits, loaded from balance.json.</summary>
public sealed record BalanceData(double DefenceConstant, Bands Bands, Fight[] Fights, HeroClass[] Classes)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static BalanceData FromJson(string json) => JsonSerializer.Deserialize<BalanceData>(json, Options)!;

    public static BalanceData Load(string? path = null) =>
        FromJson(File.ReadAllText(path ?? Path.Combine(AppContext.BaseDirectory, "balance.json")));

    /// <summary>A copy with one Path changed: the "one parameter" experiments in the tests.</summary>
    public BalanceData EditPath(string className, string pathName, Func<PathMod, PathMod> change) =>
        this with
        {
            Classes = Classes.Select(c => c.Name != className ? c
                : c with { Paths = c.Paths.Select(p => p.Name == pathName ? change(p) : p).ToArray() }).ToArray(),
        };
}
