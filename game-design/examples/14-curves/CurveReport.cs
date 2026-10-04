using System.Globalization;
using System.Text;

namespace Course.Curves;

public static class CurveReport
{
    /// <summary>A pacing table: every region with its hours and target, then the milestones and a sample of levels.</summary>
    public static string Format(CurveData d)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Region     Levels   Hours  Target  Start hour");
        foreach (var r in d.Regions)
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{r.Name,-10} {r.FromLevel,2}-{r.ToLevel,-4} {Curves.RegionHours(d, r),6:F1} {r.TargetHours,6:F1} {Curves.HoursToReach(d, r.FromLevel),9:F1}"));
        sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"Total to level {d.MaxLevel}: {Curves.TotalHours(d):F1} h (target {d.TotalTargetHours:F0} +/- {d.ToleranceHours:F0})"));
        foreach (var m in d.Milestones)
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{m.Name} (level {m.Level}) reached at hour {Curves.HoursToReach(d, m.Level):F1}"));
        sb.AppendLine("Level  XP to next  Kill XP  XP per hour  Minutes  Power ratio");
        foreach (int l in new[] { 1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 49 })
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{l,5} {Curves.XpToNext(d, l),11:N0} {Curves.KillXp(d, l),8:N0} {Curves.XpPerHour(d, l),12:N0} {Curves.HoursForLevel(d, l) * 60,8:F1} {Curves.PowerRatio(d, l),11:F2}"));
        return sb.ToString();
    }
}
