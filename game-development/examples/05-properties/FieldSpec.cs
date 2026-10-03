namespace Course.Properties;

/// <summary>One column of a table schema: name, type, whether it may be absent, and an optional numeric range.</summary>
public sealed record FieldSpec(string Name, FieldKind Kind, bool Required = true, double? Min = null, double? Max = null);
