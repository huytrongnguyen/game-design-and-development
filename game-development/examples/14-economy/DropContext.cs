namespace Course.Economy;

/// <summary>
/// Knobs that scale every denominator at roll time: a level-difference penalty (in thousandths,
/// 1000 = neutral, 800 = 25% better odds, 3000 = three times rarer) and an event rate multiplier
/// (2 = a "double drop" weekend halves every denominator).
/// </summary>
public readonly record struct DropContext(int PenaltyPermille, int RateMultiplier)
{
    public static DropContext Neutral => new(1000, 1);

    public int Effective(int denominator)
    {
        if (denominator <= 1) return 1;
        var scaled = (long)denominator * PenaltyPermille / 1000 / RateMultiplier;
        return (int)Math.Max(1, scaled);
    }
}
