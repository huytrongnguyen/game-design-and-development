using Xunit.Abstractions;

namespace Course.Ttk;

public class TtkTests(ITestOutputHelper output)
{
    private static readonly CombatData Data = CombatData.Load();

    private static CombatData WithModel(CombatData d, DefenceModel m) => d with { Defence = d.Defence with { Model = m } };

    private static CombatData HalfDefence(CombatData d) =>
        d with { Enemies = d.Enemies.Select(e => e with { Defence = e.Defence / 2 }).ToArray() };

    private static TtkRow Row(IEnumerable<TtkRow> rows, string enemy, string group) =>
        rows.Single(r => r.Enemy == enemy && r.Group == group);

    private static string[] OutOfBand(CombatData d) =>
        TtkReport.Build(d).Where(r => !r.InBand).Select(r => $"{r.Enemy}/{r.Group}").ToArray();

    [Fact]
    public void Data_Loads_From_Json()
    {
        Assert.Equal(DefenceModel.Ratio, Data.Defence.Model);
        Assert.Equal(5, Data.Classes.Length);
        Assert.Equal(new[] { "Trash pack", "Elite", "Boss" }, Data.Enemies.Select(e => e.Name));
        Assert.Equal(new[] { "Warden", "Ranger", "Arcanist", "Cleric" }, Data.Party);
    }

    [Fact]
    public void Defence_Formulas_Behave_As_Designed()
    {
        var sub = Data.Defence with { Model = DefenceModel.Subtractive };
        Assert.Equal(10.0, Formulas.ApplyDefence(sub, 40, 30), 6);   // flat subtraction
        Assert.Equal(4.4, Formulas.ApplyDefence(sub, 22, 30), 6);    // floored at 20% of the hit
        Assert.Equal(50.0, Formulas.ApplyDefence(Data.Defence, 100, 100), 6);   // ratio: K/(K+def) = 0.5
        Assert.True(Formulas.ApplyDefence(Data.Defence, 38, 1000) > 0);         // ratio never reaches zero
    }

    [Fact]
    public void Expected_Ttk_Matches_Hand_Calculation()
    {
        var rows = TtkReport.Build(Data);
        // Warden vs trash pack: average swing 38 * 0.9 + 57 * 0.1 = 39.9 raw, * 100/110 = 36.27 damage per second.
        Assert.Equal(13.2, Row(rows, "Trash pack", "Warden").Expected, 0.05);   // 480 / 36.27
        Assert.Equal(57.4, Row(rows, "Elite", "Arcanist").Expected, 0.05);
        Assert.Equal(78.4, Row(rows, "Elite", "Cleric").Expected, 0.05);
        Assert.Equal(40.2, Row(rows, "Elite", "Party").Expected, 0.05);          // 2200 * 2.5 HP over four heroes
        Assert.Equal(128.7, Row(rows, "Boss", "Party").Expected, 0.05);
    }

    [Fact]
    public void Default_Design_Has_Every_Row_In_Its_Band()
    {
        Assert.Empty(OutOfBand(Data));
    }

    [Fact]
    public void Simulation_Agrees_With_The_Formula_Within_Half_An_Interval()
    {
        // A swing lands at the end of its interval, so the simulation runs slightly above the formula.
        foreach (var r in TtkReport.Build(Data))
        {
            double interval = r.Group == "Party" ? 1.0 : Data.Class(r.Group).AttackInterval;
            Assert.InRange(r.Sim.Mean - r.Expected, 0, interval);
            Assert.True(r.Sim.P90 >= r.Sim.Mean);
        }
    }

    [Fact]
    public void Simulation_Is_Deterministic_For_A_Seed()
    {
        Assert.Equal(TtkReport.Build(Data, 500, 42).Select(r => r.Sim.Mean),
                     TtkReport.Build(Data, 500, 42).Select(r => r.Sim.Mean));
        Assert.NotEqual(TtkReport.Build(Data, 500, 42)[0].Sim.Mean, TtkReport.Build(Data, 500, 43)[0].Sim.Mean);
    }

    [Fact]
    public void Switching_To_Subtractive_Defence_Breaks_The_Small_Hit_Classes()
    {
        var sub = TtkReport.Build(WithModel(Data, DefenceModel.Subtractive));
        // Same defence numbers, different formula: the Duelist's 22-damage swings are mostly eaten by 20 defence.
        var duelist = Row(sub, "Elite", "Duelist");
        Assert.Equal(153.0, duelist.Expected, 0.05);        // was 58.8 s with ratio defence
        Assert.False(duelist.InBand);
        // The Arcanist's big 70-damage hits barely notice: still in band.
        Assert.True(Row(sub, "Elite", "Arcanist").InBand);
        Assert.Equal(63.6, Row(sub, "Elite", "Arcanist").Expected, 0.05);
        Assert.Equal(11, sub.Count(r => !r.InBand));      // 11 of 18 rows fail
    }

    [Fact]
    public void Subtractive_Defence_Can_Be_Rescaled_But_Still_Hurts_Small_Hits()
    {
        // Halve every enemy defence to compensate: only the Boss row stays broken, worst for the Duelist.
        var rescaled = HalfDefence(WithModel(Data, DefenceModel.Subtractive));
        var rows = TtkReport.Build(rescaled);
        Assert.Equal(new[] { "Boss/Warden", "Boss/Cleric", "Boss/Duelist" }, OutOfBand(rescaled));
        Assert.Equal(326.4, Row(rows, "Boss", "Duelist").Expected, 0.05);
        Assert.Equal(173.7, Row(rows, "Boss", "Arcanist").Expected, 0.05);
    }

    [Fact]
    public void Formula_And_Simulation_Disagree_When_The_Damage_Floor_Bites()
    {
        var sub = TtkReport.Build(WithModel(Data, DefenceModel.Subtractive));
        var wardenBoss = Row(sub, "Boss", "Warden");
        // The spreadsheet ignores variance, but the floor makes damage convex in the roll: lucky rolls add more than unlucky ones remove.
        Assert.Equal(656.6, wardenBoss.Expected, 0.05);
        Assert.True(wardenBoss.Expected - wardenBoss.Sim.Mean > 20);
    }

    [Fact]
    public void Party_Hp_Scale_Decides_Whether_Grouping_Is_Faster()
    {
        var rows = TtkReport.Build(Data);
        // x2.5 HP: the party kills the elite in 40.2 s against 57-78 s alone.
        Assert.True(Row(rows, "Elite", "Party").Expected < 0.75 * Row(rows, "Elite", "Arcanist").Expected);
        // x4.0 HP (scaling HP with head count): the party is slower than a solo Arcanist (57.4 s) and leaves its band.
        var tough = TtkReport.Build(Data with { PartyHpScale = 4.0 });
        Assert.Equal(64.3, Row(tough, "Elite", "Party").Expected, 0.05);
        Assert.False(Row(tough, "Elite", "Party").InBand);
    }

    [Fact]
    public void Time_To_Die_And_Required_Avoidance()
    {
        var rows = TtkReport.Build(Data);
        var w = Row(rows, "Elite", "Warden");
        Assert.Equal(41.4, w.Ttd!.Value, 0.05);           // 1300 HP / (110 * 100/140 / 2.5 s)
        Assert.Equal(0.37, w.Avoidance!.Value, 0.01);     // must avoid about 37% of the elite's damage
        Assert.Equal(0.0, Row(rows, "Trash pack", "Warden").Avoidance!.Value);   // survives the pack without dodging
    }

    [Fact]
    public void Prints_The_Balance_Table()
    {
        output.WriteLine(TtkReport.Format(TtkReport.Build(Data)));
    }
}
