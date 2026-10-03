using Xunit.Abstractions;

namespace Course.Balance;

public class BalanceTests(ITestOutputHelper output)
{
    private static readonly BalanceData Data = BalanceData.Load();

    private static PathRow Pair(BalanceData d, string cls) => Sheet.Paths(d).Single(r => r.Class == cls);
    private static ClassRow Row(BalanceData d, string cls) => Sheet.Classes(d).Single(r => r.Name == cls);
    private static BalanceData StormcallerArea(double areaMult) =>
        Data.EditPath("Arcanist", "Stormcaller", p => p with { AreaMult = areaMult });

    [Fact]
    public void Data_Loads_From_Json()
    {
        Assert.Equal(new[] { "Warden", "Cleric", "Duelist", "Ranger", "Arcanist" }, Data.Classes.Select(c => c.Name));
        Assert.Equal(new[] { "Boss", "Pack", "Dungeon" }, Data.Fights.Select(f => f.Name));
        Assert.All(Data.Classes, c => Assert.Equal(2, c.Paths.Length));
        Assert.All(Data.Fights, f => Assert.Equal(1.0, f.Weights.Dps + f.Weights.Survivability + f.Weights.Utility, 9));
        output.WriteLine(Sheet.Render(Data));
    }

    [Fact]
    public void Dps_And_Ehp_Match_A_Hand_Calculation()
    {
        var boss = Data.Fights[0];
        var ranger = Data.Classes.Single(c => c.Name == "Ranger");
        // Average swing 30 x (0.75 + 0.25 x 1.75) = 35.625 raw; x 100/130 against defence 30 = 27.40; / 0.8 s = 34.25.
        Assert.Equal(34.25, Model.Dps(Data, ranger, boss), 2);
        // Pack of 4: area bonus 1 + 0.15 x (4 - 1) = 1.45, and defence 10 instead of 30: 35.625 x 100/110 / 0.8 x 1.45 = 58.7.
        Assert.Equal(58.7, Model.Dps(Data, ranger, Data.Fights[1]), 1);
        // Warden: 1,300 HP x 140/100 / (1 - 0.05 mitigation) = 1,915.8.
        Assert.Equal(1915.8, Model.Ehp(Data, Data.Classes.Single(c => c.Name == "Warden")), 1);
    }

    [Fact]
    public void Scores_Are_Relative_To_The_Roster_Mean()
    {
        foreach (var (f, i) in Data.Fights.Select((f, i) => (f, i)))
            Assert.Equal(1.0, Sheet.Classes(Data).Average(r => r.Scores[i]), 9);   // weights sum to 1, so the mean is 1
        Assert.Equal(1.043, Row(Data, "Warden").Scores[0], 3);                      // Warden in the boss fight
    }

    [Fact]
    public void Default_Roster_Is_Inside_The_Class_Band()
    {
        var rows = Sheet.Classes(Data);
        Assert.All(rows, r => Assert.True(Sheet.InBand(Data, r), $"{r.Name} {r.Overall:F3}"));
        Assert.Equal("Duelist", rows.MinBy(r => r.Overall)!.Name);
        Assert.Equal("Arcanist", rows.MaxBy(r => r.Overall)!.Name);
        Assert.Equal(0.941, rows.Min(r => r.Overall), 3);
        Assert.Equal(1.060, rows.Max(r => r.Overall), 3);
    }

    [Fact]
    public void Exactly_One_Path_Pair_Is_Out_Of_Band_In_The_Default_Data()
    {
        var bad = Sheet.Paths(Data).Where(r => !r.InBand).ToArray();
        var storm = Assert.Single(bad);
        Assert.Equal("Stormcaller vs Frostbinder", storm.Label);
        Assert.Equal(0.003, storm.Gaps[0], 3);   // boss: a single target, area does not matter
        Assert.Equal(0.076, storm.Gaps[1], 3);   // pack: Stormcaller 7.6% ahead
        Assert.Equal(0.051, storm.Gaps[2], 3);   // dungeon: 5.1%, just over the line
        Assert.Equal(-0.032, Pair(Data, "Ranger").Gaps[1], 3);   // the widest in-band gap
    }

    [Fact]
    public void Nerfing_Stormcaller_Area_Fixes_The_Pair_And_Only_Moves_Multi_Target_Fights()
    {
        var fixedData = StormcallerArea(1.4);
        var gaps = Pair(fixedData, "Arcanist").Gaps;
        Assert.True(Pair(fixedData, "Arcanist").InBand);
        Assert.Equal(new[] { 0.003, 0.031, 0.020 }, gaps.Select(g => Math.Round(g, 3)));
        Assert.Equal(Pair(Data, "Arcanist").Gaps[0], gaps[0], 9);   // the boss fight is untouched
        // Stormcaller's own pack DPS falls from 102.9 to 95.5 (-7.2%).
        var arc = Data.Classes.Single(c => c.Name == "Arcanist");
        Assert.Equal(102.9, Model.Dps(Data, arc, Data.Fights[1], arc.Paths[0]), 1);
        Assert.Equal(95.5, Model.Dps(fixedData, arc, Data.Fights[1], fixedData.Classes[4].Paths[0]), 1);
    }

    [Fact]
    public void The_Band_Edge_Is_Between_1_45_And_1_50()
    {
        Assert.True(Pair(StormcallerArea(1.45), "Arcanist").InBand);    // pack gap +4.2%
        Assert.False(Pair(StormcallerArea(1.50), "Arcanist").InBand);   // pack gap +5.3%
    }

    [Fact]
    public void Buffing_Frostbinder_Also_Fixes_The_Pair_But_Moves_The_Boss_Gap()
    {
        var buffed = Data.EditPath("Arcanist", "Frostbinder", p => p with { UtilityDelta = 2 });
        var pair = Pair(buffed, "Arcanist");
        Assert.True(pair.InBand);
        Assert.Equal(new[] { -0.032, 0.026, 0.005 }, pair.Gaps.Select(g => Math.Round(g, 3)));   // now Frostbinder leads the boss
    }

    [Fact]
    public void Buffing_One_Class_Lowers_Everyone_Elses_Relative_Score()
    {
        var buffed = Data with
        {
            Classes = Data.Classes.Select(c => c.Name == "Duelist" ? c with { DamagePerHit = c.DamagePerHit * 1.1 } : c).ToArray(),
        };
        Assert.Equal(0.941, Row(Data, "Duelist").Overall, 3);
        Assert.Equal(0.979, Row(buffed, "Duelist").Overall, 3);
        foreach (var name in new[] { "Warden", "Cleric", "Ranger", "Arcanist" })
            Assert.True(Row(buffed, name).Overall < Row(Data, name).Overall, name);   // the mean moved up
        Assert.Equal(1.0, Sheet.Classes(buffed).Average(r => r.Overall), 9);          // the average is always 1
    }
}
