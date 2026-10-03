namespace Course.Economy;

/// <summary>One slot per body part. Equipping swaps with whatever is worn, through the inventory slot just freed.</summary>
public sealed class Equipment(ItemCatalog catalog)
{
    private readonly Dictionary<EquipSlot, ItemStack> _worn = new();

    public ItemStack? Get(EquipSlot slot) => _worn.GetValueOrDefault(slot);

    public bool TryEquip(Inventory inventory, int inventorySlot)
    {
        var stack = inventory.Get(inventorySlot);
        if (stack is null || !catalog.TryGet(stack.ItemId, out var def) || def.Slot == EquipSlot.None)
            return false;

        inventory.Take(inventorySlot);
        if (_worn.TryGetValue(def.Slot, out var previous))
            inventory.TryPlace(inventorySlot, previous); // the slot was just emptied, so this fits
        _worn[def.Slot] = stack;
        return true;
    }
}
