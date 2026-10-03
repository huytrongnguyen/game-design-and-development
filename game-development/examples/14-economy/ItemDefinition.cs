namespace Course.Economy;

/// <summary>
/// The static description of a kind of item: one row in a data table, shared by every copy.
/// It never changes while the game runs. <see cref="MaxStack"/> of 1 means each copy is
/// unique and becomes its own <see cref="ItemStack"/> with an instance id.
/// </summary>
public sealed record ItemDefinition(string Id, string Name, int MaxStack, EquipSlot Slot = EquipSlot.None, int SellPrice = 0)
{
    public bool IsStackable => MaxStack > 1;
}
