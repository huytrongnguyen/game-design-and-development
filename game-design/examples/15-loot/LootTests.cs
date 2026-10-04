using Xunit.Abstractions;

namespace Course.Loot;

public class LootTests(ITestOutputHelper output)
{
    private static readonly LootData Data = LootData.Load();

    [Fact]
    public void Data_Loads_From_Json()
    {
        Assert.Equal(new[] { "Common", "Uncommon", "Rare", "Epic" }, Data.Rarities.Select(r => r.Name));
        Assert.Equal(8, Data.Slots.Length);
        Assert.Equal(9.0, Data.Slots.Sum(s => s.Weight), 6);
        Assert.Equal(10, Data.Upgrade.Steps.Length);
        output.WriteLine(LootReport.Format(Data));
    }

    [Fact]
    public void Item_Power_And_Gear_Score_Match_Hand_Calculation()
    {
        Assert.Equal(47, Gear.StatBudget(Data, 32));                                  // 10 * (1 + 0.12 * 31) = 47.2
        Assert.Equal(114, Gear.ItemPower(Data, "Weapon", 32, "Rare", 4));              // 2.0 * 47 * 1.10 * 1.10 = 113.74
        Assert.Equal(424, Gear.GearScore(Data, 32, "Uncommon", 0));                    // 9.0 * 47 = 423, 424 after rounding each item
        int rare4 = Gear.GearScore(Data, 32, "Rare", 4), epic10 = Gear.GearScore(Data, 32, "Epic", 10);
        Assert.Equal(511, rare4);                                                      // about 1.21 x the Uncommon set
        Assert.Equal(633, epic10);                                                     // 1.50 x: the ceiling of one generation
        Assert.InRange(rare4 / 424.0, 1.20, 1.22);
        Assert.InRange(epic10 / 424.0, 1.49, 1.51);
    }

    [Fact]
    public void Drop_Table_Probabilities_Come_From_The_Weights()
    {
        Assert.Equal(0.15, Drops.RarityChance(Data, "Boss chest", "Epic"), 9);          // 15 / (85 + 15)
        Assert.Equal(0.01, Drops.RarityChance(Data, "Elite", "Epic"), 9);               // 1 / 100
        Assert.Equal(0.01875, Drops.ChancePerRoll(Data), 9);                            // 15% Epic, then 1 of 8 pieces
        // One in 53 runs: a trash enemy dropping an Epic is impossible by design (weight 0).
        Assert.Equal(0.0, Drops.RarityChance(Data, "Trash enemy", "Epic"));
    }

    [Fact]
    public void Without_Protection_The_Chase_Is_Geometric_With_A_Long_Tail()
    {
        double p = Drops.ChancePerRun(Data);
        var exact = Drops.Exact(Data, false);
        Assert.Equal(1 / p, exact.Mean, 0.01);                                          // 53.33 runs
        Assert.Equal(Math.Ceiling(Math.Log(0.10) / Math.Log(1 - p)), exact.P90);        // 122
        Assert.Equal(122, exact.P90);
        Assert.Equal(244, exact.P99);
        Assert.Equal(37, exact.P50);
        // Simulation agrees with the formula.
        var sim = Drops.Simulate(Data, false, 40_000, 3);
        Assert.Equal(exact.Mean, sim.Mean, exact.Mean * 0.03);
    }

    [Fact]
    public void Protection_Cuts_The_Tail_Much_More_Than_The_Mean()
    {
        var off = Drops.Exact(Data, false);
        var on = Drops.Exact(Data, true);
        Assert.Equal(38.2, on.Mean, 0.1);
        Assert.Equal(69, on.P90);
        Assert.Equal(83, on.P99);
        Assert.True(on.P99 <= Data.Dungeon.Pity.HardAtRuns);                            // the worst case has a ceiling
        Assert.True(on.P99 < off.P99 / 2.5);                                            // tail: 83 vs 244
        Assert.True(on.Mean > off.Mean * 0.65);                                         // mean: 38 vs 53, a smaller change
        var sim = Drops.Simulate(Data, true, 40_000, 5);
        Assert.Equal(on.Mean, sim.Mean, on.Mean * 0.03);
        Assert.True(sim.P99 <= 100);
    }

    [Fact]
    public void Tuning_Change_A_Bonus_Roll_Halves_The_Chase_Without_Touching_The_Weights()
    {
        var twoRolls = Data with { Dungeon = Data.Dungeon with { RollsPerRun = 2 } };
        double p = Drops.ChancePerRun(twoRolls);
        Assert.Equal(1 - 0.98125 * 0.98125, p, 1e-9);                                   // 3.72% per run
        Assert.Equal(26.9, Drops.Exact(twoRolls, false).Mean, 0.1);
        var exact = Drops.Exact(twoRolls, false);
        Assert.Equal(19, exact.P50);
        Assert.Equal(61, exact.P90);
        Assert.Equal(122, exact.P99);
    }

    [Fact]
    public void Tuning_Change_A_Tighter_Hard_Pity_Lowers_The_Ceiling()
    {
        var tight = Data with { Dungeon = Data.Dungeon with { Pity = Data.Dungeon.Pity with { HardAtRuns = 60 } } };
        var exact = Drops.Exact(tight, true);
        Assert.Equal(35.9, exact.Mean, 0.05);
        Assert.Equal(60, exact.P99);
    }

    [Fact]
    public void Upgrade_Cost_Closed_Form_Matches_Hand_Calculation()
    {
        // Steps 1-3 are certain: 50 + 60 + 70 = 180 gold. No step can drop the item below +7, so the cost is cost / rate.
        Assert.Equal(180.0, Upgrades.ExpectedCost(Data, 3, false), 6);
        Assert.Equal(180 + 100 / 0.85 + 130 / 0.75 + 170 / 0.65 + 220 / 0.55, Upgrades.ExpectedCost(Data, 7, false), 6);
        // Step 8: (280 + 0.55 * T7) / 0.45, where T7 = 220 / 0.55 = 400 -> 1,111.1 gold for that step alone.
        double t8 = (280 + 0.55 * 400) / 0.45;
        Assert.Equal(Upgrades.ExpectedCost(Data, 7, false) + t8, Upgrades.ExpectedCost(Data, 8, false), 6);
        Assert.Equal(1111.11, t8, 0.01);
    }

    [Fact]
    public void Upgrade_Simulation_Agrees_With_The_Closed_Form()
    {
        foreach (bool charm in new[] { false, true })
        {
            double exact = Upgrades.ExpectedCost(Data, 10, charm);
            double sim = Upgrades.Simulate(Data, 10, charm, 40_000, 9).Mean;
            Assert.Equal(exact, sim, exact * 0.03);
        }
    }

    [Fact]
    public void Charm_Costs_A_Little_More_Early_And_Saves_A_Lot_At_The_Top()
    {
        // Up to +8 the charm is a small premium, at +10 it halves the expected cost and cuts the tail by about three.
        Assert.Equal(16_298.0, Upgrades.ExpectedCost(Data, 10, false), 1.0);
        Assert.Equal(8_186.0, Upgrades.ExpectedCost(Data, 10, true), 1.0);
        Assert.True(Upgrades.ExpectedCost(Data, 8, true) > Upgrades.ExpectedCost(Data, 8, false));
        var none = Upgrades.Simulate(Data, 10, false, 20_000, 1);
        var charm = Upgrades.Simulate(Data, 10, true, 20_000, 1);
        Assert.True(charm.P99 < none.P99 / 2.5);
        Assert.True(none.P99 > 3 * none.Mean);                                          // a few unlucky players pay four times the average
    }

    [Fact]
    public void Tuning_Change_Cheaper_Charm_Moves_The_Break_Even_Down()
    {
        var cheap = Data with { Upgrade = Data.Upgrade with { CharmGold = 200 } };
        Assert.True(Upgrades.ExpectedCost(cheap, 8, true) < Upgrades.ExpectedCost(cheap, 8, false));   // 2,200 against 2,244
        Assert.Equal(6_371.0, Upgrades.ExpectedCost(cheap, 10, true), 1.0);
    }
}
