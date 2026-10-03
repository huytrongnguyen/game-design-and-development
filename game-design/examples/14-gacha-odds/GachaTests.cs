using Xunit.Abstractions;

namespace Course.GachaOdds;

public class GachaTests(ITestOutputHelper output)
{
    private static readonly GachaData Data = GachaData.Load();
    private static readonly Model Course = Data.Model("Pity and 50/50");
    private static readonly Model NoPity = Data.Model("No pity, 50/50 each time");

    [Fact]
    public void Data_Loads_From_Json()
    {
        Assert.Equal(4, Data.Models.Length);
        Assert.Equal(1.6, Data.UsdPerPull, 9);   // 160 units per pull, 100 units per dollar
        Assert.Equal(90, Course.HardPity);
        Assert.Equal(0.5, Course.FeaturedChance);
    }

    [Fact]
    public void Rate_Per_Pull_Follows_The_Pity_Rules()
    {
        Assert.Equal(0.006, Course.Rate(1), 9);
        Assert.Equal(0.006, Course.Rate(73), 9);     // last pull before soft pity
        Assert.Equal(0.066, Course.Rate(74), 9);     // base + one step
        Assert.Equal(0.966, Course.Rate(89), 9);     // base + 16 steps
        Assert.Equal(1.0, Course.Rate(90), 9);       // hard pity
        Assert.Equal(0.006, NoPity.Rate(500), 9);    // no pity: the rate never changes
    }

    [Fact]
    public void One_Top_Result_Costs_About_62_Pulls_With_Pity()
    {
        var top = Odds.TopResult(Course, Data.Horizon);
        Assert.Equal(1.0, top.Mass, 9);
        Assert.InRange(top.Mean, 62.2, 62.4);
        Assert.Equal(76, top.Percentile(0.5));
        Assert.Equal(1 / 0.006, Odds.TopResult(NoPity, Data.Horizon).Mean, 0);   // 166.7 without pity
    }

    [Fact]
    public void The_50_50_Rule_Raises_The_Average_By_Half()
    {
        var top = Odds.TopResult(Course, Data.Horizon);
        var featured = Odds.Featured(Course, Data.Horizon);
        Assert.Equal(1.5 * top.Mean, featured.Mean, 6);              // 1.5 top results per featured item
        Assert.InRange(featured.Mean, 93.3, 93.6);
        Assert.Equal(80, featured.Percentile(0.5));
        Assert.Equal(155, featured.Percentile(0.9));
        Assert.Equal(161, featured.Percentile(0.99));
        Assert.InRange(featured.ProbMoreThan(90), 0.40, 0.41);       // 4 in 10 are still missing it after one full pity
    }

    [Fact]
    public void Hard_Pity_Bounds_The_Worst_Case()
    {
        var featured = Odds.Featured(Course, Data.Horizon);
        Assert.Equal(180, featured.WorstCase);                       // two full pity runs
        Assert.True(featured.ProbMoreThan(180) < 1e-12);
        var sim = Simulator.Run(Course, Data.Trials, Data.Seed);
        Assert.True(sim.Max <= 180);
        Assert.Equal(0, sim.ShareOverHardCase);

        // Without pity there is no bound: more than half of players need over 180 pulls, and some need thousands.
        var loose = Odds.Featured(NoPity, Data.Horizon);
        Assert.InRange(loose.ProbMoreThan(180), 0.55, 0.62);
        Assert.Equal(-1, loose.WorstCase);
        Assert.True(Simulator.Run(NoPity, Data.Trials, Data.Seed).Max > 1000);
    }

    [Fact]
    public void Seeded_Simulation_Agrees_With_The_Closed_Form()
    {
        foreach (var m in Data.Models)
        {
            var exact = Odds.Featured(m, Data.Horizon);
            var sim = Simulator.Run(m, Data.Trials, Data.Seed);
            output.WriteLine($"{m.Name}: exact mean {exact.Mean:F2} sim {sim.Mean:F2}; p90 {exact.Percentile(0.9)} / {sim.P90}");
            Assert.InRange(sim.Mean / exact.Mean, 0.99, 1.01);
            Assert.InRange(sim.Median - exact.Percentile(0.5), -2, 2);
            Assert.InRange(sim.P90 - exact.Percentile(0.9), -4, 4);
        }
    }

    [Fact]
    public void Expected_Spend_In_Dollars()
    {
        var featured = Odds.Featured(Course, Data.Horizon);
        Assert.InRange(Odds.Spend(Data, featured.Mean), 149, 150);                 // about $150 on average
        Assert.Equal(248, Odds.Spend(Data, featured.Percentile(0.9)), 6);          // 1 in 10 pays $248 or more
        Assert.Equal(288, Odds.Spend(Data, featured.WorstCase), 6);                // the hard cap: 180 pulls
        var guaranteed = Odds.Featured(Data.Model("Pity, always featured"), Data.Horizon);
        Assert.InRange(Odds.Spend(Data, guaranteed.Mean), 99, 100);                // dropping the 50/50 saves a third
        Assert.Equal(90, guaranteed.WorstCase);
        var lowPity = Odds.Featured(Data.Model("2% with hard pity 50"), Data.Horizon);
        Assert.InRange(lowPity.Mean, 31.7, 31.9);                                  // (1 - 0.98^50) / 0.02
        Assert.Equal(80, Odds.Spend(Data, lowPity.WorstCase), 6);
    }

    [Fact]
    public void Summary_For_Every_Model()
    {
        // (median, p90, p99, worst case) in pulls; worst case -1 means unbounded.
        var expected = new Dictionary<string, (int Median, int P90, int P99, int Worst)>
        {
            ["Pity and 50/50"] = (80, 155, 161, 180),
            ["Pity, always featured"] = (76, 80, 83, 90),
            ["No pity, 50/50 each time"] = (231, 767, 1533, -1),
            ["2% with hard pity 50"] = (35, 50, 50, 50),
        };
        foreach (var m in Data.Models)
        {
            var f = Odds.Featured(m, Data.Horizon);
            var e = expected[m.Name];
            output.WriteLine($"{m.Name}: ${Odds.Spend(Data, f.Mean):F0} mean, ${Odds.Spend(Data, f.Percentile(0.9)):F0} at p90");
            Assert.Equal(e, (f.Percentile(0.5), f.Percentile(0.9), f.Percentile(0.99), f.WorstCase));
        }
    }

    [Fact]
    public void Models_Rank_By_Average_Cost()
    {
        var means = Data.Models.Select(m => Odds.Featured(m, Data.Horizon).Mean).ToArray();
        Assert.True(means[3] < means[1] && means[1] < means[0] && means[0] < means[2]);
        Assert.InRange(means[2] / means[0], 3.4, 3.7);   // no pity costs about 3.6x
    }

    [Fact]
    public void Same_Seed_Same_Result_Different_Seed_Differs()
    {
        var a = Simulator.Run(Course, 20_000, 1);
        var b = Simulator.Run(Course, 20_000, 1);
        var c = Simulator.Run(Course, 20_000, 2);
        Assert.Equal(a, b);
        Assert.NotEqual(a.Mean, c.Mean);
    }
}
