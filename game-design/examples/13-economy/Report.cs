using System.Globalization;
using System.Text;

namespace Course.Economy;

public static class Report
{
    /// <summary>A plain-text table of the run, one line per sampled day.</summary>
    public static string Table(EconomyRun run, params int[] days)
    {
        var sb = new StringBuilder();
        sb.AppendLine("day   active   supply/active   faucet/day   sink/day   sink ratio   days held");
        foreach (int n in days)
        {
            var r = run.Day(n);
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{r.Day,3}  {r.Active,7:N0}  {r.SupplyPerActive,13:N0}  {r.FaucetPerActive,11:N0}  {r.SinkPerActive,9:N0}  {r.SinkRatio,10:P0}  {r.DaysOfIncomeHeld,9:F1}"));
        }
        return sb.ToString();
    }
}
