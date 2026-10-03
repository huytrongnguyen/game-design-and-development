namespace Course.Progression;

/// <summary>Four primary stats. Used for class growth per circle, free-point allocation and totals.</summary>
public readonly record struct StatBlock(int Str = 0, int Agi = 0, int Vit = 0, int Int = 0)
{
    public static StatBlock operator +(StatBlock a, StatBlock b) => new(a.Str + b.Str, a.Agi + b.Agi, a.Vit + b.Vit, a.Int + b.Int);
    public static StatBlock operator *(StatBlock a, int k) => new(a.Str * k, a.Agi * k, a.Vit * k, a.Int * k);
    public int Total => Str + Agi + Vit + Int;
}

public enum Stat { Str, Agi, Vit, Int }

public static class StatBlockExtensions
{
    public static int Get(this StatBlock s, Stat stat) => stat switch
    {
        Stat.Str => s.Str, Stat.Agi => s.Agi, Stat.Vit => s.Vit, Stat.Int => s.Int,
        _ => throw new ArgumentOutOfRangeException(nameof(stat)),
    };

    public static StatBlock With(this StatBlock s, Stat stat, int value) => stat switch
    {
        Stat.Str => s with { Str = value }, Stat.Agi => s with { Agi = value },
        Stat.Vit => s with { Vit = value }, Stat.Int => s with { Int = value },
        _ => throw new ArgumentOutOfRangeException(nameof(stat)),
    };
}
