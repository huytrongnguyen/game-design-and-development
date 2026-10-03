namespace Course.Economy.Tests;

internal static class TestItems
{
    public static ItemCatalog Catalog() => new(
    [
        new ItemDefinition("potion", "Potion", MaxStack: 10, SellPrice: 50),
        new ItemDefinition("sword", "Sword", MaxStack: 1, EquipSlot.Weapon),
        new ItemDefinition("axe", "Axe", MaxStack: 1, EquipSlot.Weapon),
        new ItemDefinition("ore", "Ore", MaxStack: 99),
    ]);

    public static Inventory Inventory(int slots) => new(slots, Catalog(), new InstanceIdGenerator());
}
