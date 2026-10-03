using System.Text.Json;

namespace Course.GachaOdds;

/// <summary>
/// One way to run a draw. A top-rarity result has probability <c>BaseRate</c> per pull; from pull
/// <c>SoftPityStart</c> the rate rises by <c>SoftPityStep</c> per pull (the first boosted pull already has one step);
/// at pull <c>HardPity</c> a top-rarity result is guaranteed. Zero means "no such rule".
/// A top-rarity result is the featured item with chance <c>FeaturedChance</c>; after a miss, the next one is
/// the featured item for sure when <c>GuaranteeAfterLoss</c> is true. The pity counter resets after every top-rarity result.
/// </summary>
public sealed record Model(
    string Name, double BaseRate, int SoftPityStart, double SoftPityStep, int HardPity,
    double FeaturedChance, bool GuaranteeAfterLoss)
{
    /// <summary>Chance that pull number <paramref name="k"/> (1-based, counted since the last top-rarity result) is a top-rarity result.</summary>
    public double Rate(int k)
    {
        if (HardPity > 0 && k >= HardPity) return 1.0;
        if (SoftPityStart > 0 && k >= SoftPityStart) return Math.Min(1.0, BaseRate + SoftPityStep * (k - SoftPityStart + 1));
        return BaseRate;
    }
}

public sealed record GachaData(double PullCost, double UnitsPerUsd, int Horizon, int Trials, ulong Seed, Model[] Models)
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static GachaData FromJson(string json) => JsonSerializer.Deserialize<GachaData>(json, Options)!;

    public static GachaData Load(string? path = null) =>
        FromJson(File.ReadAllText(path ?? Path.Combine(AppContext.BaseDirectory, "gacha.json")));

    public Model Model(string name) => Models.Single(m => m.Name == name);

    /// <summary>Price of one pull in dollars.</summary>
    public double UsdPerPull => PullCost / UnitsPerUsd;
}
