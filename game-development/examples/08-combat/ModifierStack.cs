namespace Course.Combat;

/// <summary>
/// Layered modifiers for one stat:
/// <c>(base + flat) * (1 + sum of percentAdd) * product of (1 + percentMult) + finalFlat</c>.
/// Percentages are written as whole numbers (10 = +10%).
/// </summary>
public sealed class ModifierStack
{
    private readonly List<StatModifier> _mods = [];

    /// <summary>Bumps on every change so a cache can tell when it is stale.</summary>
    public int Version { get; private set; }

    public void Add(StatModifier modifier)
    {
        _mods.Add(modifier);
        Version++;
    }

    public int RemoveSource(string source)
    {
        int removed = _mods.RemoveAll(m => m.Source == source);
        if (removed > 0) Version++;
        return removed;
    }

    public double Evaluate(double baseValue)
    {
        double flat = 0, percentAdd = 0, finalFlat = 0, mult = 1;
        foreach (var m in _mods)
        {
            switch (m.Layer)
            {
                case ModifierLayer.Flat: flat += m.Value; break;
                case ModifierLayer.PercentAdd: percentAdd += m.Value; break;
                case ModifierLayer.PercentMult: mult *= 1 + m.Value / 100; break;
                case ModifierLayer.FinalFlat: finalFlat += m.Value; break;
            }
        }
        return (baseValue + flat) * (1 + percentAdd / 100) * mult + finalFlat;
    }
}
