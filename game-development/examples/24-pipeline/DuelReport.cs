namespace M24.Pipeline;

public sealed record DuelReport(int Duels, int WinsA, int WinsB, int Draws)
{
    public double WinRateA => (double)WinsA / Duels;
}
