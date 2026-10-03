namespace Course.Properties;

/// <summary>
/// The property store of one game object. Base values are written directly; calculated values are computed on first
/// read, cached, and thrown away when something they depend on changes.
/// </summary>
public sealed class PropertyBag
{
    private readonly PropertySchema _schema;
    private readonly Dictionary<string, double> _base = new();
    private readonly Dictionary<string, double> _cache = new();
    private readonly Dictionary<string, int> _recomputes = new();
    private readonly Stack<HashSet<string>> _reads = new();

    /// <param name="strict">When true, a formula that reads an undeclared property throws instead of going stale.</param>
    public PropertyBag(PropertySchema schema, bool strict = false)
    {
        _schema = schema;
        Strict = strict;
    }

    public bool Strict { get; }

    /// <summary>Total number of formula evaluations. A test can prove that no more work was done than necessary.</summary>
    public int RecomputeCount { get; private set; }

    public int RecomputesOf(string name) => _recomputes.GetValueOrDefault(name);

    public bool IsCached(string name) => _cache.ContainsKey(name);

    public void Set(string name, double value)
    {
        if (_schema.Get(name).IsCalculated)
            throw new InvalidOperationException($"'{name}' is calculated and cannot be set");

        if (_base.TryGetValue(name, out var old) && old == value) return;
        _base[name] = value;
        Invalidate(name);
    }

    public double Get(string name)
    {
        if (_reads.Count > 0) _reads.Peek().Add(name);

        var def = _schema.Get(name);
        if (!def.IsCalculated)
            return _base.TryGetValue(name, out var b) ? b : throw new InvalidOperationException($"base property '{name}' has no value");

        if (_cache.TryGetValue(name, out var cached)) return cached;
        return Compute(def);
    }

    private double Compute(PropertyDefinition def)
    {
        _reads.Push(new HashSet<string>());
        double value;
        HashSet<string> reads;
        try
        {
            value = def.Formula!(this);
        }
        finally
        {
            reads = _reads.Pop();
        }

        if (Strict)
            foreach (var read in reads)
                if (!def.DependsOn.Contains(read))
                    throw new UndeclaredDependencyException(def.Name, read);

        RecomputeCount++;
        _recomputes[def.Name] = _recomputes.GetValueOrDefault(def.Name) + 1;
        _cache[def.Name] = value;
        return value;
    }

    // Walk the reverse map. A property that is already uncached has had its dependents cleared too, so we can stop there.
    private void Invalidate(string changed)
    {
        foreach (var dependent in _schema.DependentsOf(changed))
            if (_cache.Remove(dependent))
                Invalidate(dependent);
    }
}
