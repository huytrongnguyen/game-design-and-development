namespace Course.Skills;

/// <summary>
/// Static data for one buff or debuff. Effects are declared (modifiers, periodic damage), never coded per buff name.
/// </summary>
/// <param name="Modifiers">Stat modifiers contributed per stack while the buff is active.</param>
/// <param name="PeriodTicks">0 for no periodic effect; otherwise the effect fires every PeriodTicks.</param>
/// <param name="PeriodicDamage">Damage per period, per stack.</param>
public sealed record BuffDefinition(
    string Id,
    int DurationTicks,
    StackingPolicy Policy,
    int MaxStacks,
    bool IsDebuff,
    bool Dispellable,
    IReadOnlyList<StatModifier> Modifiers,
    int PeriodTicks = 0,
    int PeriodicDamage = 0);
