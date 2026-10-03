namespace Course.Scripting;

/// <summary>A dictionary-backed <see cref="IPropertyLookup"/>, enough for tests and small tools.</summary>
public sealed class MapProperties : IPropertyLookup
{
    private readonly Dictionary<string, double> _values;

    public MapProperties(IDictionary<string, double> values) =>
        _values = new Dictionary<string, double>(values, StringComparer.Ordinal);

    public IReadOnlySet<string> Names => _values.Keys.ToHashSet(StringComparer.Ordinal);

    public bool TryGet(string name, out double value) => _values.TryGetValue(name, out value);
}
