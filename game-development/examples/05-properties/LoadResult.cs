namespace Course.Properties;

/// <summary>Either a valid table or a complete list of everything wrong with the file. Never a half-loaded table.</summary>
public sealed record LoadResult(DataTable? Table, IReadOnlyList<ValidationError> Errors)
{
    public bool Ok => Table is not null;
}
