namespace Course.Properties;

/// <summary>The contract a data file must satisfy: table name and the allowed columns.</summary>
public sealed class TableSchema
{
    public TableSchema(string name, IEnumerable<FieldSpec> fields)
    {
        Name = name;
        Fields = fields.ToList();
    }

    public string Name { get; }

    public IReadOnlyList<FieldSpec> Fields { get; }
}
