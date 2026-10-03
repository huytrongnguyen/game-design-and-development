namespace Course.GameLoop;

/// <summary>
/// A deterministic random source, seeded explicitly and injected into the simulation,
/// never read from <c>Random.Shared</c> or any other process-global/time-based source.
/// Two <see cref="SeededRng"/> instances created with the same seed, and asked for the
/// same sequence of rolls, always produce the same sequence of numbers — a prerequisite
/// for replay (see <c>ReplayTests</c>).
/// </summary>
public sealed class SeededRng
{
    private readonly Random _random;

    public int Seed { get; }

    public SeededRng(int seed)
    {
        Seed = seed;
        _random = new Random(seed);
    }

    /// <summary>Returns an integer in [1, sides], inclusive, like rolling a die.</summary>
    public int RollDie(int sides)
    {
        if (sides < 1)
            throw new ArgumentOutOfRangeException(nameof(sides), "A die must have at least one side.");

        return _random.Next(1, sides + 1);
    }
}
