namespace Course.Economy;

/// <summary>One row of the experience-by-level-difference table: differences up to
/// <paramref name="MaxDiff"/> (monster level minus player level) use <paramref name="Permille"/>.</summary>
public readonly record struct XpBand(int MaxDiff, int Permille);

/// <summary>
/// Experience from a kill: the same-level monster value times a multiplier chosen by how many
/// levels the monster is above (+) or below (-) the player. The bands are data, ordered by
/// <see cref="XpBand.MaxDiff"/>; the last band catches everything higher.
/// </summary>
public sealed class KillXp(IReadOnlyList<XpBand> bands, int outOfRangePermille)
{
    /// <summary>Sample bands: grey (10+ below) 0.30, low (5-9 below) 0.70, even 1.00, tough (5-10 above) 1.20, beyond 1.10.</summary>
    public static KillXp Sample { get; } = new(
        [new(-10, 300), new(-5, 700), new(4, 1000), new(10, 1200)], 1100);

    public long Gain(int monsterXpAtOwnLevel, int monsterLevel, int playerLevel)
    {
        var diff = monsterLevel - playerLevel;
        var permille = outOfRangePermille;
        foreach (var band in bands)
            if (diff <= band.MaxDiff) { permille = band.Permille; break; }
        return (long)monsterXpAtOwnLevel * permille / 1000;
    }
}
