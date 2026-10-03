namespace Course.Properties;

/// <summary>
/// A property is either a base value (no formula) or a calculated value (a formula plus the names it depends on).
/// The dependency list is declared by hand, exactly like a data column, not discovered by running the formula.
/// </summary>
public sealed record PropertyDefinition(string Name, Func<PropertyBag, double>? Formula, IReadOnlyList<string> DependsOn)
{
    public bool IsCalculated => Formula is not null;

    public static PropertyDefinition Base(string name) => new(name, null, Array.Empty<string>());

    public static PropertyDefinition Calculated(string name, Func<PropertyBag, double> formula, params string[] dependsOn) =>
        new(name, formula, dependsOn);
}
