using System.Globalization;
using System.Text;

namespace Course.Ttk;

/// <summary>One line of the balance table: one group (a class alone, or the party) against one enemy.</summary>
public sealed record TtkRow(
    string Enemy, string Group, double Expected, SimResult Sim,
    double BandMin, double BandMax, double? Ttd, double? Avoidance)
{
    public bool InBand => Expected >= BandMin && Expected <= BandMax;
}

public static class TtkReport
{
    /// <summary>Every class solo (enemy HP x1) and the configured party (enemy HP x PartyHpScale), per enemy.</summary>
    public static List<TtkRow> Build(CombatData d, int trials = 2000, ulong seed = 7)
    {
        var rows = new List<TtkRow>();
        int n = 0;
        foreach (var e in d.Enemies)
        {
            foreach (var c in d.Classes)
            {
                var team = new[] { c };
                double ttk = Formulas.ExpectedTtk(d.Defence, team, e.Hp, e.Defence);
                double ttd = Formulas.TimeToDie(d.Defence, c, e);
                rows.Add(new TtkRow(e.Name, c.Name, ttk,
                    Simulator.Run(d.Defence, team, e.Hp, e.Defence, trials, seed + (ulong)n++),
                    e.Bands.Solo[0], e.Bands.Solo[1], ttd, Formulas.RequiredAvoidance(ttk, ttd)));
            }
            var party = d.Party.Select(d.Class).ToArray();
            double hp = e.Hp * d.PartyHpScale;
            rows.Add(new TtkRow(e.Name, "Party", Formulas.ExpectedTtk(d.Defence, party, hp, e.Defence),
                Simulator.Run(d.Defence, party, hp, e.Defence, trials, seed + (ulong)n++),
                e.Bands.Party[0], e.Bands.Party[1], null, null));
        }
        return rows;
    }

    private static string Opt(double? v, string format, string unit) =>
        v is { } x ? (x.ToString(format, CultureInfo.InvariantCulture) + unit).PadLeft(6) : "-".PadLeft(6);

    public static string Format(IEnumerable<TtkRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Enemy       Group    Expected  SimMean  SimP90   Band         TTD   Avoid  In band");
        foreach (var r in rows)
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{r.Enemy,-11} {r.Group,-8} {r.Expected,7:F1}s {r.Sim.Mean,7:F1}s {r.Sim.P90,6:F1}s " +
                          $"{r.BandMin,4:F0}-{r.BandMax,-4:F0}s {Opt(r.Ttd, "F1", "s")} {Opt(r.Avoidance, "P0", "")}  {(r.InBand ? "yes" : "NO")}"));
        return sb.ToString();
    }
}
