namespace Course.Properties;

/// <summary>One validated record. Values are already typed, so game code never parses strings.</summary>
public sealed class DataRow
{
    private readonly Dictionary<string, object> _values;

    public DataRow(string id, Dictionary<string, object> values)
    {
        Id = id;
        _values = values;
    }

    public string Id { get; }

    public double GetNumber(string field) => (double)_values[field];

    public string GetText(string field) => (string)_values[field];

    public bool Has(string field) => _values.ContainsKey(field);
}
