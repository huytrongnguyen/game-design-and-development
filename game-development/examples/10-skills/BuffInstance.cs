namespace Course.Skills;

/// <summary>A running copy of a <see cref="BuffDefinition"/> on one unit.</summary>
public sealed class BuffInstance(BuffDefinition definition, int appliedAt)
{
    public BuffDefinition Definition { get; } = definition;
    public int Stacks { get; internal set; } = 1;

    /// <summary>The first tick at which the buff is no longer active (applied tick + duration).</summary>
    public int ExpiresAt { get; internal set; } = appliedAt + definition.DurationTicks;

    /// <summary>The next tick a periodic effect is due.</summary>
    public int NextPeriodAt { get; internal set; } = appliedAt + definition.PeriodTicks;
}
