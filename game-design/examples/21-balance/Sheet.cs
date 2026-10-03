using System.Globalization;
using System.Text;

namespace Course.Balance;

/// <summary>One class on the sheet. Scores are per fight, in the order of the data file's fights.</summary>
public sealed record ClassRow(string Name, string Role, double[] Dps, double Ehp, double Utility, double[] Scores)
{
    public double Overall => Scores.Average();
}

/// <summary>One Path pair. Gap = score of the first Path / score of the second - 1, per fight.</summary>
public sealed record PathRow(string Class, string PathA, string PathB, double[] Gaps, double Limit)
{
    public bool InBand => Gaps.All(g => Math.Abs(g) <= Limit);
    public string Label => $"{PathA} vs {PathB}";
}

public static class Sheet
{
    public static ClassRow[] Classes(BalanceData d) => d.Classes.Select(c => new ClassRow(
        c.Name, c.Role,
        d.Fights.Select(f => Model.Dps(d, c, f)).ToArray(),
        Model.Ehp(d, c), c.Utility,
        d.Fights.Select(f => Model.Score(d, c, f)).ToArray())).ToArray();

    public static bool InBand(BalanceData d, ClassRow r) =>
        r.Overall >= d.Bands.ClassScore[0] && r.Overall <= d.Bands.ClassScore[1];

    public static PathRow[] Paths(BalanceData d) => d.Classes.Select(c =>
    {
        var (a, b) = (c.Paths[0], c.Paths[1]);
        var gaps = d.Fights.Select(f => Model.Score(d, c, f, a) / Model.Score(d, c, f, b) - 1).ToArray();
        return new PathRow(c.Name, a.Name, b.Name, gaps, d.Bands.PathGap);
    }).ToArray();

    private static string F(double v, string format) => v.ToString(format, CultureInfo.InvariantCulture);

    public static string Render(BalanceData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Class      DPS(" + string.Join("/", d.Fights.Select(f => f.Name)) + ")      EHP   Util  Score(per fight)        Overall");
        foreach (var r in Classes(d))
            sb.AppendLine($"{r.Name,-10} {string.Join("/", r.Dps.Select(x => F(x, "F1"))),-22} {F(r.Ehp, "F0"),5}  {F(r.Utility, "F1"),4}  " +
                          $"{string.Join(" ", r.Scores.Select(x => F(x, "F3"))),-22} {F(r.Overall, "F3")} {(InBand(d, r) ? "ok" : "OUT")}");
        sb.AppendLine("Path pair                      Gap per fight (first / second - 1)");
        foreach (var p in Paths(d))
            sb.AppendLine($"{p.Class,-9} {p.Label,-24} {string.Join(" ", p.Gaps.Select(g => F(g, "+0.0%;-0.0%")))}  {(p.InBand ? "ok" : "OUT")}");
        return sb.ToString();
    }
}
