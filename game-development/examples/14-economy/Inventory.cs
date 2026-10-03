namespace Course.Economy;

/// <summary>
/// A fixed number of slots. Stackable items share a slot up to their max stack; unique items
/// take one slot each. Adding is all-or-nothing: if the whole request does not fit, nothing changes.
/// </summary>
public sealed class Inventory(int slotCount, ItemCatalog catalog, InstanceIdGenerator ids)
{
    private readonly ItemStack?[] _slots = new ItemStack?[slotCount];

    public int SlotCount => _slots.Length;

    public ItemStack? Get(int index) => _slots[index];

    public int CountOf(string itemId) => _slots.Where(s => s?.ItemId == itemId).Sum(s => s!.Count);

    public AddResult TryAdd(string itemId, int count)
    {
        if (count < 1) return AddResult.InvalidCount;
        if (!catalog.TryGet(itemId, out var def)) return AddResult.UnknownItem;

        // Check capacity first so a failed add leaves the inventory untouched.
        long room = 0;
        foreach (var s in _slots)
        {
            if (s is null) room += def.MaxStack;
            else if (s.ItemId == itemId && def.IsStackable) room += def.MaxStack - s.Count;
        }
        if (count > room) return AddResult.NoSpace;

        var remaining = count;
        for (var i = 0; i < _slots.Length && remaining > 0 && def.IsStackable; i++)
        {
            var s = _slots[i];
            if (s is null || s.ItemId != itemId || s.Count >= def.MaxStack) continue;
            var moved = Math.Min(remaining, def.MaxStack - s.Count);
            _slots[i] = s with { Count = s.Count + moved };
            remaining -= moved;
        }
        for (var i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i] is not null) continue;
            var moved = Math.Min(remaining, def.MaxStack);
            _slots[i] = new ItemStack(itemId, moved, def.IsStackable ? null : ids.Next());
            remaining -= moved;
        }
        return AddResult.Added;
    }

    /// <summary>Removes <paramref name="count"/> of an item, emptying later stacks first. All-or-nothing.</summary>
    public bool TryRemove(string itemId, int count)
    {
        if (count < 1 || CountOf(itemId) < count) return false;
        var remaining = count;
        for (var i = _slots.Length - 1; i >= 0 && remaining > 0; i--)
        {
            var s = _slots[i];
            if (s is null || s.ItemId != itemId) continue;
            var taken = Math.Min(remaining, s.Count);
            _slots[i] = taken == s.Count ? null : s with { Count = s.Count - taken };
            remaining -= taken;
        }
        return true;
    }

    /// <summary>Lifts a whole stack out of a slot (used when equipping).</summary>
    public ItemStack? Take(int index)
    {
        var s = _slots[index];
        _slots[index] = null;
        return s;
    }

    /// <summary>Puts a stack into an empty slot.</summary>
    public bool TryPlace(int index, ItemStack stack)
    {
        if (_slots[index] is not null) return false;
        _slots[index] = stack;
        return true;
    }
}
