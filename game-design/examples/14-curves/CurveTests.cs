using Xunit.Abstractions;

namespace Course.Curves;

public class CurveTests(ITestOutputHelper output)
{
    private static readonly CurveData Data = CurveData.Load();

    [Fact]
    public void Data_Loads_From_Json()
    {
        Assert.Equal(50, Data.MaxLevel);
        Assert.Equal("polynomial", Data.Xp.Kind);
        Assert.Equal(4, Data.Regions.Length);
        Assert.Equal(new[] { 20, 40 }, Data.Milestones.Select(m => m.Level));
        output.WriteLine(CurveReport.Format(Data));
    }

    [Fact]
    public void Xp_Table_Matches_Hand_Calculation()
    {
        Assert.Equal(250, Curves.XpToNext(Data, 1));
        Assert.Equal(54_928, Curves.XpToNext(Data, 20));     // 250 * 20^1.8
        Assert.Equal(KillXpAt20, Curves.KillXp(Data, 20));   // 8 * 20^1.2
        Assert.Equal(25 * KillXpAt20, Curves.QuestXp(Data, 20));
    }

    private const long KillXpAt20 = 291;

    [Fact]
    public void Hours_Per_Level_Follow_The_Plan()
    {
        // Level 20 (region 2): XP per hour = 291 * (240 kills + 6 quests * 25) = 113,490; bar 54,845 -> 0.48 h.
        Assert.Equal(113_490, Curves.XpPerHour(Data, 20), 0.5);
        Assert.Equal(29.0, Curves.HoursForLevel(Data, 20) * 60, 0.5);
        // The first level is a short win; the last ones are the longest.
        Assert.InRange(Curves.HoursForLevel(Data, 1) * 60, 4, 6);
        Assert.InRange(Curves.HoursForLevel(Data, 49) * 60, 45, 55);
        for (int l = 2; l < 50; l++)
            Assert.True(Curves.HoursForLevel(Data, l) * 60 <= Curves.HoursForLevel(Data, l + 1 > 49 ? 49 : l + 1) * 60 * 1.15,
                $"level {l} should not be far longer than the next");
    }

    [Fact]
    public void Total_Time_To_Level_50_Is_25_Hours_Within_Tolerance()
    {
        Assert.InRange(Curves.TotalHours(Data), 24, 26);
        foreach (var r in Data.Regions)
            Assert.InRange(Curves.RegionHours(Data, r), r.TargetHours - 0.4, r.TargetHours + 0.4);
    }

    [Fact]
    public void Path_And_Mastery_Arrive_At_The_Planned_Hours()
    {
        Assert.InRange(Curves.HoursToReach(Data, 20), 5.2, 6.2);    // Path: after about three evenings of 2 h
        Assert.InRange(Curves.HoursToReach(Data, 40), 17.0, 19.0);  // Mastery: about three quarters of the way
    }

    [Fact]
    public void Party_Bonus_Shortens_The_Climb()
    {
        // The 25 hours are the solo baseline. At the 1.84x reward rate of module 11 the same XP takes 25 / 1.84 hours.
        Assert.Equal(Curves.TotalHours(Data) / 1.84, Curves.TotalHours(Data, 1.84), 6);
        Assert.InRange(Curves.TotalHours(Data, 1.84), 13, 14.5);
    }

    [Fact]
    public void Deriving_The_Table_From_A_Time_Budget_Round_Trips()
    {
        // Budget: every level takes 20 minutes. Derive the XP table, then measure the hours it implies.
        long[] table = Curves.DeriveXpTable(Data, _ => 20.0 / 60);
        Assert.Equal(49, table.Length);
        Assert.Equal(Math.Round(20.0 / 60 * Curves.XpPerHour(Data, 20)), table[19]);   // 37,830
        Assert.Equal(37_830, table[19]);
        double hours = table.Select((xp, i) => xp / Curves.XpPerHour(Data, i + 1)).Sum();
        Assert.Equal(49 * 20.0 / 60, hours, 0.01);
    }

    [Fact]
    public void Curve_Kinds_Behave_Differently()
    {
        var linear = Data with { Xp = new XpCurve("linear", 250, Slope: 600) };
        var expo = Data with { Xp = new XpCurve("exponential", 250, Growth: 1.13) };
        Assert.Equal(12_250, Curves.XpToNext(linear, 20));              // 250 + 600 * 20
        Assert.Equal(2_549, Curves.XpToNext(expo, 20));                 // 250 * 1.13^19
        // Linear XP cannot keep up with income that grows faster than the bar: levels get shorter, not longer.
        Assert.True(Curves.HoursForLevel(linear, 49) < Curves.HoursForLevel(linear, 1));
        Assert.True(Curves.TotalHours(linear) < 6);
        // Exponential XP is lopsided: level 10 takes under a minute, level 49 takes about 15 times longer than level 10.
        Assert.True(Curves.HoursForLevel(expo, 49) > 10 * Curves.HoursForLevel(expo, 10));
        Assert.True(Curves.TotalHours(expo) < 4);
    }

    [Fact]
    public void Tuning_Change_Steeper_Exponent_Moves_Hours_To_The_Late_Game()
    {
        var steeper = Data with { Xp = Data.Xp with { Exponent = 1.9 } };
        double before = Curves.RegionHours(Data, Data.Regions[3]), after = Curves.RegionHours(steeper, steeper.Regions[3]);
        output.WriteLine($"Exponent 1.8 -> 1.9: total {Curves.TotalHours(Data):F1} -> {Curves.TotalHours(steeper):F1} h, region 4 {before:F1} -> {after:F1} h");
        Assert.True(Curves.TotalHours(steeper) > 27);
        Assert.True(after / before > Curves.RegionHours(steeper, steeper.Regions[0]) / Curves.RegionHours(Data, Data.Regions[0]));
    }

    [Fact]
    public void Cap_Raise_Adds_A_Region_And_About_Eight_And_A_Half_Hours()
    {
        var raised = Data with
        {
            MaxLevel = 60,
            Regions = Data.Regions.Append(new Region("Expansion", 50, 59, 300, 4, 0)).ToArray(),
        };
        double added = Curves.TotalHours(raised) - Curves.TotalHours(Data);
        output.WriteLine($"Cap 50 -> 60 on the same curve adds {added:F1} h");
        Assert.InRange(added, 8.0, 9.5);
    }

    [Fact]
    public void Rested_Pool_Rewards_The_Short_Daily_Session()
    {
        // Level 20, 23 h offline: pool = 23% of a 54,845 bar = 12,633 XP. A 20-minute session earns ~37,830 base XP,
        // so the bonus cap (50% = 18,915) is higher than the pool and the pool is the limit.
        Assert.Equal(12_633.0, Curves.RestedExtraXp(Data, 20, 23, 37_830), 1.0);
        // A two-hour session after 22 h offline earns ~226,980 base XP: the same pool is only a 5% top-up.
        Assert.Equal(0.053, Curves.RestedExtraXp(Data, 20, 22, 226_980) / 226_980, 0.001);
        // The pool is capped at 72% of a bar however long the player is away.
        Assert.Equal(0.72 * 54_928, Curves.RestedExtraXp(Data, 20, 500, 1_000_000), 1.0);
    }

    [Fact]
    public void Power_Ratio_Stays_In_Band_And_Saws_Through_The_Regions()
    {
        for (int l = 1; l <= 50; l++)
            Assert.InRange(Curves.PowerRatio(Data, l), Data.Power.Band[0], Data.Power.Band[1]);
        Assert.Equal(1.00, Curves.PowerRatio(Data, 1), 0.001);
        Assert.Equal(1.20 * 2.20 / 2.08, Curves.PowerRatio(Data, 11), 0.001);   // gear 1.20, level power 2.20 over the hub level 10 power 2.08
        Assert.True(Curves.PowerRatio(Data, 11) > Curves.PowerRatio(Data, 12));  // a new region resets the ratio
        Assert.True(Curves.PowerRatio(Data, 24) > Curves.PowerRatio(Data, 12));
    }
}
