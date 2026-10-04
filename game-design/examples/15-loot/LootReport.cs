using System.Globalization;
using System.Text;

namespace Course.Loot;

public static class LootReport
{
    public static string Format(LootData d, int trials = 20000, ulong seed = 11)
    {
        var sb = new StringBuilder();
        void Line(FormattableString s) => sb.AppendLine(s.ToString(CultureInfo.InvariantCulture));
        Line($"Target: one specific {d.Dungeon.TargetRarity} piece of {d.Dungeon.Pieces} from {d.Dungeon.Name}; chance per run {Drops.ChancePerRun(d):P3}");
        Line($"Runs to target       Mean    P50    P90    P99");
        foreach (var (label, prot) in new[] { ("No protection (exact)", false), ("Protection (exact)", true) })
        {
            var s = Drops.Exact(d, prot);
            Line($"{label,-21} {s.Mean,6:F1} {s.P50,6:F0} {s.P90,6:F0} {s.P99,6:F0}");
        }
        var sim = Drops.Simulate(d, true, trials, seed);
        Line($"{"Protection (sim)",-21} {sim.Mean,6:F1} {sim.P50,6:F0} {sim.P90,6:F0} {sim.P99,6:F0}");
        Line($"");
        Line($"Gold to upgrade   Closed form   Sim mean     P90      P99");
        foreach (int n in new[] { 3, 5, 7, 8, 9, 10 })
            foreach (bool charm in new[] { false, true })
            {
                var s = Upgrades.Simulate(d, n, charm, trials, seed);
                Line($"+0 -> +{n,-2} {(charm ? "charm" : "none "),-7} {Upgrades.ExpectedCost(d, n, charm),10:N0} {s.Mean,10:N0} {s.P90,8:N0} {s.P99,8:N0}");
            }
        return sb.ToString();
    }
}
