namespace M24.Pipeline;

/// <summary>
/// Headless balance simulation: many duels, each with its own seed derived from a base seed,
/// so a report is exactly reproducible and a surprising single duel can be replayed alone.
/// </summary>
public static class DuelSim
{
    public const int MaxRounds = 100;

    public static DuelReport Run(Fighter a, Fighter b, int duels, int baseSeed)
    {
        int winsA = 0, winsB = 0, draws = 0;
        for (var i = 0; i < duels; i++)
        {
            switch (Duel(a, b, new Random(baseSeed + i)))
            {
                case 1: winsA++; break;
                case -1: winsB++; break;
                default: draws++; break;
            }
        }

        return new DuelReport(duels, winsA, winsB, draws);
    }

    /// <summary>Returns 1 if A wins, -1 if B wins, 0 for a draw. A strikes first each round.</summary>
    public static int Duel(Fighter a, Fighter b, Random rng)
    {
        int hpA = a.Hp, hpB = b.Hp;
        for (var round = 0; round < MaxRounds; round++)
        {
            hpB -= Hit(a, b, rng);
            if (hpB <= 0)
            {
                return 1;
            }

            hpA -= Hit(b, a, rng);
            if (hpA <= 0)
            {
                return -1;
            }
        }

        return 0;
    }

    private static int Hit(Fighter attacker, Fighter defender, Random rng)
    {
        var damage = Math.Max(1, attacker.Attack - defender.Defense);
        return rng.Next(100) < attacker.CritPercent ? damage * 2 : damage;
    }
}
