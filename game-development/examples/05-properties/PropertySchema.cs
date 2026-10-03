namespace Course.Properties;

/// <summary>
/// The shared, immutable-after-setup description of one kind of object: which properties exist and who depends on whom.
/// It builds the reverse map (property to its dependents) once, so invalidation is a plain lookup at run time.
/// </summary>
public sealed class PropertySchema
{
    private readonly Dictionary<string, PropertyDefinition> _defs = new();
    private readonly Dictionary<string, List<string>> _dependents = new();

    public IReadOnlyCollection<PropertyDefinition> Definitions => _defs.Values;

    public PropertyDefinition Get(string name) =>
        _defs.TryGetValue(name, out var d) ? d : throw new KeyNotFoundException($"unknown property '{name}'");

    public IReadOnlyList<string> DependentsOf(string name) =>
        _dependents.TryGetValue(name, out var list) ? list : Array.Empty<string>();

    /// <summary>Adds a property. Throws <see cref="PropertyCycleException"/> if the new declaration closes a loop.</summary>
    public PropertySchema Add(PropertyDefinition def)
    {
        if (_defs.ContainsKey(def.Name))
            throw new InvalidOperationException($"property '{def.Name}' is already registered");

        _defs[def.Name] = def;
        var path = FindCycle(def.Name);
        if (path is not null)
        {
            _defs.Remove(def.Name);
            throw new PropertyCycleException(path);
        }

        foreach (var dep in def.DependsOn)
        {
            if (!_dependents.TryGetValue(dep, out var list))
                _dependents[dep] = list = new List<string>();
            list.Add(def.Name);
        }

        return this;
    }

    /// <summary>Names every declared dependency that was never registered. Call once after all Add calls.</summary>
    public IReadOnlyList<string> FindUnknownDependencies() =>
        _defs.Values.SelectMany(d => d.DependsOn).Where(n => !_defs.ContainsKey(n)).Distinct().ToList();

    // Depth-first walk along "depends on" edges from the new node; reaching it again means a loop.
    private List<string>? FindCycle(string start)
    {
        var path = new List<string> { start };
        return Visit(start, start, path) ? path : null;
    }

    private bool Visit(string current, string start, List<string> path)
    {
        foreach (var dep in _defs[current].DependsOn)
        {
            if (dep == start)
            {
                path.Add(dep);
                return true;
            }

            if (!_defs.ContainsKey(dep)) continue;
            path.Add(dep);
            if (Visit(dep, start, path)) return true;
            path.RemoveAt(path.Count - 1);
        }

        return false;
    }
}
