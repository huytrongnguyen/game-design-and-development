namespace Course.Properties;

/// <summary>A problem found while loading data. Names the table, the row and the column so a designer can fix it.</summary>
public sealed record ValidationError(string Table, string RowId, string Field, string Message)
{
    public override string ToString() => $"{Table}[{RowId}].{Field}: {Message}";
}
