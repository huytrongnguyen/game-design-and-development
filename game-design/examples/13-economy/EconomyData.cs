using System.Text.Json;

namespace Course.Economy;

/// <summary>From this day on, this many new players start per day.</summary>
public sealed record NewPlayerSegment(int FromDay, double PerDay);

/// <summary>A point on the leveling curve: after this many hours played, the hero is at this level (linear in between).</summary>
public sealed record LevelPoint(double Hours, int Level);

/// <summary>Links hours played to a level and a level to the "stage" factor that scales income and prices.
/// Stage = level power relative to the reference level, so it is 1.0 at <c>ReferenceLevel</c>.</summary>
public sealed record Progress(double LevelPower, int ReferenceLevel, LevelPoint[] LevelByHour);

/// <summary>A gold source. When <c>MaxLevel</c> is set, the source dries up once the hero reaches that level
/// and its hours move proportionally to the other sources (finite content, such as quests).</summary>
public sealed record Faucet(string Name, double GoldPerHour, int? MaxLevel = null);

/// <summary>A kind of player: its share of the base, hours played per day, and how those hours split across faucets.</summary>
public sealed record Profile(string Name, double Share, double HoursPerDay, Dictionary<string, double> Mix);

/// <summary>A gold drain. Cost per use, times uses per active hour plus uses per day.</summary>
public sealed record Sink(string Name, double Cost, double UsesPerHour, double UsesPerDay, bool ScalesWithStage);

/// <summary>Gold value of items a player sells through the market per active hour, and the fee the seller pays.</summary>
public sealed record Market(double VolumePerHour, double Fee);

/// <summary>Everything a designer edits, loaded from economy.json.</summary>
public sealed record EconomyData(
    int Days, double RetentionExponent, NewPlayerSegment[] NewPlayers, Progress Progress,
    Faucet[] Faucets, Profile[] Profiles, Sink[] Sinks, Market Market)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static EconomyData FromJson(string json) => JsonSerializer.Deserialize<EconomyData>(json, Options)!;

    public static EconomyData Load(string? path = null) =>
        FromJson(File.ReadAllText(path ?? Path.Combine(AppContext.BaseDirectory, "economy.json")));

    public EconomyData WithMarketFee(double fee) => this with { Market = Market with { Fee = fee } };

    public EconomyData WithSink(Sink sink) => this with { Sinks = [.. Sinks, sink] };

    public EconomyData WithoutSinks() => this with { Sinks = [], Market = Market with { Fee = 0 } };
}
