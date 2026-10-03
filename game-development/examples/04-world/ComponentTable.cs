namespace Course.World;

/// <summary>Storage for one component type, keyed by slot index. The store checks the generation before touching it.</summary>
internal sealed class ComponentTable<T> where T : struct
{
    private readonly Dictionary<int, T> _items = [];

    public void Set(int index, T value) => _items[index] = value;

    public bool TryGet(int index, out T value) => _items.TryGetValue(index, out value);

    public bool Has(int index) => _items.ContainsKey(index);

    public void Remove(int index) => _items.Remove(index);
}
