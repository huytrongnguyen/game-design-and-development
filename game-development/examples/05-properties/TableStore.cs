namespace Course.Properties;

/// <summary>
/// Holds the live table and supports hot reload: a new file replaces the old table only if it validates completely.
/// A bad edit therefore never takes the running game down; the previous table stays in force.
/// </summary>
public sealed class TableStore
{
    private readonly TableSchema _schema;

    public TableStore(TableSchema schema, string initialJson)
    {
        _schema = schema;
        var result = TableLoader.Load(schema, initialJson);
        if (!result.Ok)
            throw new InvalidOperationException("initial table is invalid: " + string.Join("; ", result.Errors));
        Current = result.Table!;
    }

    public DataTable Current { get; private set; }

    /// <summary>Number of successful swaps; lets callers notice a reload happened and rebuild what they derived.</summary>
    public int Version { get; private set; }

    public LoadResult Reload(string json)
    {
        var result = TableLoader.Load(_schema, json);
        if (result.Ok)
        {
            Current = result.Table!;
            Version++;
        }

        return result;
    }
}
