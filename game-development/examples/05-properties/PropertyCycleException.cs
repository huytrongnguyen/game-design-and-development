namespace Course.Properties;

/// <summary>Thrown at registration time when declared dependencies form a loop (A needs B needs A).</summary>
public sealed class PropertyCycleException(IReadOnlyList<string> path)
    : InvalidOperationException("dependency cycle: " + string.Join(" -> ", path))
{
    public IReadOnlyList<string> Path { get; } = path;
}
