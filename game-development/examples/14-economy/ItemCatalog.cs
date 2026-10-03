namespace Course.Economy;

/// <summary>Read-only lookup of item definitions by id (the "item table").</summary>
public sealed class ItemCatalog
{
    private readonly Dictionary<string, ItemDefinition> _byId = new();

    public ItemCatalog(IEnumerable<ItemDefinition> definitions)
    {
        foreach (var d in definitions)
        {
            if (d.MaxStack < 1)
                throw new ArgumentException($"Item '{d.Id}' must allow a stack of at least 1.");
            if (!_byId.TryAdd(d.Id, d))
                throw new ArgumentException($"Duplicate item id '{d.Id}'.");
        }
    }

    public bool TryGet(string id, out ItemDefinition definition) => _byId.TryGetValue(id, out definition!);
}
