using Xunit.Abstractions;

namespace Course.Economy;

public class EconomyTests(ITestOutputHelper output)
{
    private static readonly EconomyData Data = EconomyData.Load();
    private static readonly EconomyRun Base = Simulator.Run(Data);

    private static double Day90(EconomyData d) => Simulator.Run(d).Last.SupplyPerActive;

    [Fact]
    public void Data_Loads_From_Json()
    {
        Assert.Equal(90, Data.Days);
        Assert.Equal(4, Data.Faucets.Length);
        Assert.Equal(7, Data.Sinks.Length);
        Assert.Equal(1.0, Data.Profiles.Sum(p => p.Share), 9);
        Assert.Equal(0.05, Data.Market.Fee);
        Assert.All(Data.Profiles, p => Assert.Equal(1.0, p.Mix.Values.Sum(), 9));
    }

    [Fact]
    public void Regular_Player_Income_And_Spend_Match_Hand_Calculation()
    {
        var regular = Data.Profiles.Single(p => p.Name == "regular");
        // 1.5 h x (0.25x900 + 0.25x1100 + 0.40x1500 + 0.10x2000) = 1.5 x 1,300 gold per hour
        Assert.Equal(1950.0, Simulator.IncomeByFaucet(Data, regular, level: 30).Values.Sum(), 6);
        var want = Simulator.WantedSpend(Data, regular, stage: 1.0);
        Assert.Equal(300.0, want["potions"], 6);        // 40 x 5 per hour x 1.5 h
        Assert.Equal(1240.8, want["tempering"], 6); // 188 x 4.4 x 1.5
        Assert.Equal(90.0, want[Simulator.MarketFeeName], 6);   // 1,200 x 1.5 x 5%
        Assert.Equal(1972.8, want.Values.Sum(), 6);   // about 101% of income: a regular player at level 30 spends all of it
    }

    [Fact]
    public void Stage_Is_Tied_To_The_Leveling_Curve()
    {
        Assert.Equal(30, Simulator.LevelAt(Data, 11.3));             // hour 11 of 25.1 is about level 30
        Assert.Equal(50, Simulator.LevelAt(Data, 25.1));
        Assert.Equal(1.0, Simulator.Stage(Data, 30), 9);             // stage 1.0 is level 30
        Assert.Equal(1.536, Simulator.Stage(Data, 50), 3);           // (1 + 0.12 x 49) / (1 + 0.12 x 29)
        var regular = Data.Profiles.Single(p => p.Name == "regular");
        Assert.Equal(0, Simulator.IncomeByFaucet(Data, regular, 50)["quests"]);   // finite quests dry up at 50
        Assert.Equal(2050.0, Simulator.IncomeByFaucet(Data, regular, 50).Values.Sum(), 6);   // their hours move to the other faucets
    }

    [Fact]
    public void Active_Players_Follow_The_Retention_Curve()
    {
        Assert.Equal(3000, Base.Day(1).Active, 6);
        Assert.Equal(1.0, Simulator.Retention(Data, 0), 9);
        Assert.InRange(Simulator.Retention(Data, 30), 0.12, 0.13);   // 31^-0.6
        Assert.InRange(Base.Day(7).Active, 10_000, 11_500);          // launch spike
        Assert.InRange(Base.Last.Active, 8_800, 9_200);              // settled player base
    }

    [Fact]
    public void Day_90_Money_Supply_Per_Active_Player_Is_In_The_Design_Band()
    {
        output.WriteLine(Report.Table(Base, 1, 7, 14, 30, 45, 60, 75, 90));
        Assert.InRange(Base.Day(30).SupplyPerActive, 1_900, 2_100);
        Assert.InRange(Base.Last.SupplyPerActive, 8_000, 9_000);
        Assert.InRange(Base.Last.DaysOfIncomeHeld, 3.5, 4.3);        // target: under 5 days of income
        Assert.InRange(Base.Last.SinkRatio, 0.90, 0.93);             // target: 85-95%
        Assert.True(Base.Day(90).SupplyPerActive > Base.Day(60).SupplyPerActive);   // still drifting up slowly
    }

    [Fact]
    public void Averages_Hide_Wealth_Concentration()
    {
        Assert.Equal(0, Base.VeteranWallet["casual"], 6);            // spends everything it earns
        Assert.InRange(Base.VeteranWallet["regular"], 10_000, 14_000);
        Assert.InRange(Base.VeteranWallet["hardcore"], 90_000, 110_000);
        Assert.True(Base.VeteranWallet["hardcore"] > 7 * Base.VeteranWallet["regular"]);
    }

    [Fact]
    public void Gold_Is_Conserved_Between_Faucets_Sinks_And_Wallets()
    {
        double created = Base.Days.Sum(r => r.FaucetPerActive * r.Active);
        double destroyed = Base.Days.Sum(r => r.SinkPerActive * r.Active);
        Assert.Equal(created - destroyed, Base.Last.ActiveGold + Base.Last.DormantGold, 3);
    }

    [Fact]
    public void Day_90_Source_And_Sink_Breakdown()
    {
        var f = Base.FinalDayFaucets;
        var s = Base.FinalDaySinks;
        output.WriteLine(string.Join(", ", f.Select(kv => $"{kv.Key} {kv.Value:F0}")));
        output.WriteLine(string.Join(", ", s.Select(kv => $"{kv.Key} {kv.Value:F0}")));
        Assert.Equal(Base.Last.FaucetPerActive, f.Values.Sum(), 6);
        Assert.Equal(Base.Last.SinkPerActive, s.Values.Sum(), 6);
        Assert.True(f["dungeons"] > 0.55 * f.Values.Sum());                       // dungeons are almost 60% of all gold
        Assert.Equal("tempering", s.OrderByDescending(kv => kv.Value).First().Key);
        Assert.InRange(s["tempering"] / s.Values.Sum(), 0.62, 0.70);          // tempering takes about 66% of sinks
        Assert.InRange(s[Simulator.MarketFeeName] / f.Values.Sum(), 0.03, 0.045); // the 5% fee destroys about 3.7% of gold created
    }

    [Fact]
    public void Raising_The_Market_Fee_Lowers_Supply()
    {
        double baseline = Base.Last.SupplyPerActive;
        double fee10 = Day90(Data.WithMarketFee(0.10));
        double fee0 = Day90(Data.WithMarketFee(0));
        output.WriteLine($"fee 0%: {fee0:F0}   fee 5%: {baseline:F0}   fee 10%: {fee10:F0}");
        Assert.InRange(fee10, 4_800, 5_400);
        Assert.True(fee10 < 0.7 * baseline);
        Assert.True(fee0 > 1.3 * baseline);
        Assert.True(Simulator.Run(Data.WithMarketFee(0.10)).Last.SinkRatio > Base.Last.SinkRatio);
    }

    [Fact]
    public void Adding_A_Sink_Bends_The_Trend_Down()
    {
        var withSink = Data.WithSink(new Sink("reforge", 2000, 0, 0.1, true));
        var run = Simulator.Run(withSink);
        double growthBase = Base.Day(90).SupplyPerActive - Base.Day(60).SupplyPerActive;
        double growthNew = run.Day(90).SupplyPerActive - run.Day(60).SupplyPerActive;
        output.WriteLine($"day-90 supply {Base.Last.SupplyPerActive:F0} -> {run.Last.SupplyPerActive:F0}; growth in the last 30 days {growthBase:F0} -> {growthNew:F0}");
        Assert.True(run.Last.SupplyPerActive < 0.8 * Base.Last.SupplyPerActive);
        Assert.True(growthNew < 0.7 * growthBase);
        Assert.True(run.Last.SinkRatio > Base.Last.SinkRatio);
    }

    [Fact]
    public void Without_Sinks_The_Supply_Explodes()
    {
        var run = Simulator.Run(Data.WithoutSinks());
        Assert.Equal(0, run.Last.SinkRatio, 9);
        Assert.True(run.Last.SupplyPerActive > 8 * Base.Last.SupplyPerActive);
        Assert.InRange(run.Last.DaysOfIncomeHeld, 40, 50);           // holding over a month of income
    }

    [Fact]
    public void The_Run_Is_Deterministic()
    {
        var again = Simulator.Run(Data);
        Assert.Equal(Base.Last.SupplyPerActive, again.Last.SupplyPerActive);
    }
}
