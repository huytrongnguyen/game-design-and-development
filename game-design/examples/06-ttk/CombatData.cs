using System.Text.Json;
using System.Text.Json.Serialization;

namespace Course.Ttk;

public enum DefenceModel { Subtractive, Ratio }

/// <summary>Which formula turns a raw hit and a defence value into damage, and its constants.</summary>
public sealed record DefenceRule(DefenceModel Model, double RatioConstant, double MinFraction);

public sealed record HeroClass(
    string Name, string Role, double Hp, double Defence,
    double DamagePerHit, double AttackInterval,
    double CritChance, double CritMultiplier, double Variance);

/// <summary>Target time-to-kill windows in seconds: [min, max].</summary>
public sealed record Bands(double[] Solo, double[] Party);

public sealed record Enemy(
    string Name, double Hp, double Defence,
    int Attackers, double DamagePerHit, double AttackInterval, Bands Bands);

/// <summary>Everything a designer edits, loaded from combat.json.</summary>
public sealed record CombatData(
    DefenceRule Defence, string[] Party, double PartyHpScale,
    HeroClass[] Classes, Enemy[] Enemies)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static CombatData FromJson(string json) =>
        JsonSerializer.Deserialize<CombatData>(json, Options)!;

    public static CombatData Load(string? path = null) =>
        FromJson(File.ReadAllText(path ?? Path.Combine(AppContext.BaseDirectory, "combat.json")));

    public HeroClass Class(string name) => Classes.Single(c => c.Name == name);
}
