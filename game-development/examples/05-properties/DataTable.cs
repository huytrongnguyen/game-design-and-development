namespace Course.Properties;

/// <summary>An immutable, validated table: records looked up by id (a "type object" per row).</summary>
public sealed class DataTable
{
    private readonly Dictionary<string, DataRow> _rows;

    public DataTable(string name, IEnumerable<DataRow> rows)
    {
        Name = name;
        _rows = rows.ToDictionary(r => r.Id);
    }

    public string Name { get; }

    public int Count => _rows.Count;

    public DataRow this[string id] => _rows[id];

    public bool TryGet(string id, out DataRow row) => _rows.TryGetValue(id, out row!);
}
