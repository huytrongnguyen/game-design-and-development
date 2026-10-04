namespace Course.Loot;

/// <summary>SplitMix64: a tiny seeded generator, so a run is identical on every machine.</summary>
public sealed class Rng(ulong seed)
{
    private ulong _state = seed;

    public double NextDouble()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) / (double)(1UL << 53);
    }
}

public sealed record Spread(double Mean, double P50, double P90, double P99)
{
    public static Spread Of(double[] values)
    {
        var v = values.Order().ToArray();
        double At(double q) => v[(int)(q * (v.Length - 1))];
        return new Spread(v.Average(), At(0.5), At(0.9), At(0.99));
    }
}
