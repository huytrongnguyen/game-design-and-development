namespace Course.Combat;

/// <summary>
/// A derived stat that recomputes only when its inputs change (event-driven), not every tick.
/// Combat then reads a cached number.
/// </summary>
public sealed class CachedStat(Func<double> baseFormula)
{
    private double _value;
    private int _seenVersion = -1;
    private bool _dirty = true;

    public ModifierStack Modifiers { get; } = new();

    /// <summary>How many times the formula actually ran. Useful to prove the cache works.</summary>
    public int RecomputeCount { get; private set; }

    /// <summary>Call when an input of the base formula (level, a primary stat) changed.</summary>
    public void MarkDirty() => _dirty = true;

    public double Value
    {
        get
        {
            if (_dirty || _seenVersion != Modifiers.Version)
            {
                _value = Modifiers.Evaluate(baseFormula());
                _seenVersion = Modifiers.Version;
                _dirty = false;
                RecomputeCount++;
            }
            return _value;
        }
    }
}
