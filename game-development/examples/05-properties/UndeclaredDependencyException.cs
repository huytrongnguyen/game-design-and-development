namespace Course.Properties;

/// <summary>Thrown in strict mode when a formula reads a property it did not declare: the stale-stat bug, caught early.</summary>
public sealed class UndeclaredDependencyException(string property, string undeclared)
    : InvalidOperationException($"'{property}' read '{undeclared}' but did not declare it as a dependency")
{
    public string Property { get; } = property;

    public string Undeclared { get; } = undeclared;
}
