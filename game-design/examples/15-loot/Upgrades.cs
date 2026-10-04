namespace Course.Loot;

/// <summary>Cost of upgrading an item from +0 to +N, with and without a protection charm.</summary>
public static class Upgrades
{
    /// <summary>Gold for one attempt at step i (1-based). A charm is only bought where a failure could drop the item.</summary>
    public static double AttemptCost(LootData d, int step, bool charm) =>
        d.Upgrade.Steps[step - 1].Gold + (charm && step >= d.Upgrade.DropFromStep ? d.Upgrade.CharmGold : 0);

    /// <summary>Closed form. T(i) = expected gold to climb from +(i-1) to +i for the first time.
    /// A failure that drops the item costs a trip back up first: T(i) = (cost + (1 - p) * T(i-1)) / p. Otherwise T(i) = cost / p.</summary>
    public static double ExpectedCost(LootData d, int target, bool charm)
    {
        double previous = 0, total = 0;
        for (int i = 1; i <= target; i++)
        {
            double p = d.Upgrade.Steps[i - 1].Rate, cost = AttemptCost(d, i, charm);
            bool drops = !charm && i >= d.Upgrade.DropFromStep;
            double t = (cost + (drops ? (1 - p) * previous : 0)) / p;
            total += t;
            previous = t;
        }
        return total;
    }

    /// <summary>One simulated climb: returns the gold spent and the number of attempts.</summary>
    public static (double Gold, int Attempts) Climb(LootData d, int target, bool charm, Rng rng)
    {
        int level = 0, attempts = 0;
        double gold = 0;
        while (level < target)
        {
            int step = level + 1;
            gold += AttemptCost(d, step, charm);
            attempts++;
            if (rng.NextDouble() < d.Upgrade.Steps[step - 1].Rate) level++;
            else if (!charm && step >= d.Upgrade.DropFromStep) level--;
        }
        return (gold, attempts);
    }

    public static Spread Simulate(LootData d, int target, bool charm, int trials, ulong seed)
    {
        var rng = new Rng(seed);
        return Spread.Of(Enumerable.Range(0, trials).Select(_ => Climb(d, target, charm, rng).Gold).ToArray());
    }
}
