using System.Text.RegularExpressions;

namespace Course.Scripting;

/// <summary>
/// Maps hook names to implementations. Names follow one convention (UPPER_SNAKE_CASE, a domain prefix),
/// and data refers to hooks only by name, so the registry is the single place a name is resolved.
/// </summary>
public sealed partial class HookRegistry
{
    private readonly Dictionary<string, ContentHook> _hooks = new(StringComparer.Ordinal);

    [GeneratedRegex("^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$")]
    private static partial Regex NamePattern();

    public int Count => _hooks.Count;

    public void Register(string name, ContentHook hook)
    {
        if (!NamePattern().IsMatch(name))
            throw new ArgumentException($"Hook name '{name}' must be UPPER_SNAKE_CASE with a prefix, e.g. QUEST_DONE_WOLVES.", nameof(name));
        if (!_hooks.TryAdd(name, hook))
            throw new InvalidOperationException($"Hook '{name}' is already registered.");
    }

    public bool Contains(string name) => _hooks.ContainsKey(name);

    public bool TryGet(string name, out ContentHook hook) => _hooks.TryGetValue(name, out hook!);
}
