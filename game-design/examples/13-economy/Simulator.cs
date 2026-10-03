namespace Course.Economy;

/// <summary>One simulated day, averaged over the players who were active that day.</summary>
public sealed record DayRow(
    int Day, double Active, double ActiveGold, double SupplyPerActive,
    double FaucetPerActive, double SinkPerActive, double DormantGold)
{
    /// <summary>Share of the gold created today that was destroyed today.</summary>
    public double SinkRatio => FaucetPerActive == 0 ? 0 : SinkPerActive / FaucetPerActive;

    /// <summary>How many days of income the average active player is holding.</summary>
    public double DaysOfIncomeHeld => FaucetPerActive == 0 ? 0 : SupplyPerActive / FaucetPerActive;
}

public sealed record EconomyRun(
    IReadOnlyList<DayRow> Days,
    IReadOnlyDictionary<string, double> FinalDayFaucets,
    IReadOnlyDictionary<string, double> FinalDaySinks,
    IReadOnlyDictionary<string, double> VeteranWallet)
{
    public DayRow Day(int n) => Days[n - 1];
    public DayRow Last => Days[^1];
}

/// <summary>
/// Day-by-day money-supply model. Players join in cohorts. Each cohort has a retention curve and, per
/// player profile, an average wallet. Every day each wallet earns from the faucets, then pays the sinks
/// in order, but never spends gold it does not have. Players who leave take their wallet with them
/// (it counts as dormant gold, not as supply). The market fee destroys a share of gold that changes hands.
/// </summary>
public static class Simulator
{
    public const string MarketFeeName = "market fee";

    public static double Retention(EconomyData d, int age) => Math.Pow(1 + age, -d.RetentionExponent);

    public static double NewPlayersOn(EconomyData d, int day) =>
        d.NewPlayers.Where(s => s.FromDay <= day).OrderBy(s => s.FromDay).Last().PerDay;

    /// <summary>Level after a number of hours played, interpolated along the leveling curve and capped at its last point.</summary>
    public static int LevelAt(EconomyData d, double hours)
    {
        var pts = d.Progress.LevelByHour;
        if (hours >= pts[^1].Hours) return pts[^1].Level;
        int i = Array.FindLastIndex(pts, p => p.Hours <= hours);
        double t = (hours - pts[i].Hours) / (pts[i + 1].Hours - pts[i].Hours);
        return (int)Math.Floor(pts[i].Level + t * (pts[i + 1].Level - pts[i].Level));
    }

    /// <summary>Income and scaled prices as a multiple of what they are at the reference level (level power ratio).</summary>
    public static double Stage(EconomyData d, int level) =>
        (1 + d.Progress.LevelPower * (level - 1)) / (1 + d.Progress.LevelPower * (d.Progress.ReferenceLevel - 1));

    /// <summary>Gold per day at stage 1.0 for a profile at a level, by faucet. Faucets past their MaxLevel hand their hours to the rest.</summary>
    public static Dictionary<string, double> IncomeByFaucet(EconomyData d, Profile p, int level)
    {
        double Share(Faucet f) => p.Mix.GetValueOrDefault(f.Name);
        var open = d.Faucets.Where(f => f.MaxLevel is not int max || level < max).ToArray();
        double scale = d.Faucets.Sum(Share) / Math.Max(open.Sum(Share), 1e-9);
        return d.Faucets.ToDictionary(f => f.Name,
            f => open.Contains(f) ? f.GoldPerHour * p.HoursPerDay * Share(f) * scale : 0);
    }

    /// <summary>Gold per day that a profile wants to spend at a stage, by sink (market fee included).</summary>
    public static Dictionary<string, double> WantedSpend(EconomyData d, Profile p, double stage)
    {
        var wanted = new Dictionary<string, double>();
        foreach (var s in d.Sinks)
        {
            double uses = s.UsesPerHour * p.HoursPerDay + s.UsesPerDay;
            wanted[s.Name] = s.Cost * (s.ScalesWithStage ? stage : 1) * uses;
        }
        wanted[MarketFeeName] = d.Market.VolumePerHour * stage * p.HoursPerDay * d.Market.Fee;
        return wanted;
    }

    public static EconomyRun Run(EconomyData d)
    {
        int days = d.Days, np = d.Profiles.Length;
        var wallet = new double[days + 1, np];          // [cohort start day, profile]
        var rows = new List<DayRow>();
        double dormant = 0;
        var finalFaucets = new Dictionary<string, double>();
        var finalSinks = new Dictionary<string, double>();

        for (int day = 1; day <= days; day++)
        {
            double active = 0, gold = 0, faucetSum = 0, sinkSum = 0;
            bool last = day == days;
            for (int c = 1; c <= day; c++)
            {
                int age = day - c;
                double joined = NewPlayersOn(d, c);
                double alive = joined * Retention(d, age);
                double left = age == 0 ? 0 : joined * (Retention(d, age - 1) - Retention(d, age));
                active += alive;
                for (int p = 0; p < np; p++)
                {
                    var prof = d.Profiles[p];
                    int level = LevelAt(d, age * prof.HoursPerDay);
                    double stage = Stage(d, level);
                    double weight = alive * prof.Share;
                    dormant += left * prof.Share * wallet[c, p];       // leavers keep yesterday's wallet
                    double w = wallet[c, p];
                    foreach (var (name, perDay) in IncomeByFaucet(d, prof, level))
                    {
                        double earned = perDay * stage;
                        w += earned;
                        faucetSum += weight * earned;
                        if (last) finalFaucets[name] = finalFaucets.GetValueOrDefault(name) + weight * earned;
                    }
                    foreach (var (name, want) in WantedSpend(d, prof, stage))
                    {
                        double spent = Math.Min(w, want);
                        w -= spent;
                        sinkSum += weight * spent;
                        if (last) finalSinks[name] = finalSinks.GetValueOrDefault(name) + weight * spent;
                    }
                    wallet[c, p] = w;
                    gold += weight * w;
                }
            }
            rows.Add(new DayRow(day, active, gold, gold / active, faucetSum / active, sinkSum / active, dormant));
            if (last)
            {
                finalFaucets = finalFaucets.ToDictionary(k => k.Key, k => k.Value / active);
                finalSinks = finalSinks.ToDictionary(k => k.Key, k => k.Value / active);
            }
        }
        var veteran = d.Profiles.Select((p, i) => (p.Name, W: wallet[1, i])).ToDictionary(x => x.Name, x => x.W);
        return new EconomyRun(rows, finalFaucets, finalSinks, veteran);
    }
}
