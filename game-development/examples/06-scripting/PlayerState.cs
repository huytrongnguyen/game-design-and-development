namespace Course.Scripting;

/// <summary>Minimal player state that hooks may change, only through <see cref="HookContext"/>.</summary>
public sealed class PlayerState
{
    private readonly Dictionary<string, int> _items = new();

    public long Gold { get; internal set; }

    public int ItemCount(string itemId) => _items.GetValueOrDefault(itemId);

    internal void AddItem(string itemId, int count) =>
        _items[itemId] = _items.GetValueOrDefault(itemId) + count;
}
