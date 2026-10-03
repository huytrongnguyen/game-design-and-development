namespace Course.Economy.Tests;

public class InventoryTests
{
    [Fact]
    public void TryAdd_StackableUpToMax_FillsOneSlotThenOpensANewOne()
    {
        var inv = TestItems.Inventory(3);
        Assert.Equal(AddResult.Added, inv.TryAdd("potion", 10));
        Assert.Equal(10, inv.Get(0)!.Count);
        Assert.Null(inv.Get(1));

        Assert.Equal(AddResult.Added, inv.TryAdd("potion", 3));
        Assert.Equal(10, inv.Get(0)!.Count);
        Assert.Equal(3, inv.Get(1)!.Count);
    }

    [Fact]
    public void TryAdd_TopsUpPartialStackBeforeUsingEmptySlot()
    {
        var inv = TestItems.Inventory(3);
        inv.TryAdd("potion", 8);
        inv.TryAdd("potion", 5);
        Assert.Equal(10, inv.Get(0)!.Count);
        Assert.Equal(3, inv.Get(1)!.Count);
    }

    [Fact]
    public void TryAdd_FullInventory_RejectsAndChangesNothing()
    {
        var inv = TestItems.Inventory(2);
        inv.TryAdd("potion", 10);
        inv.TryAdd("sword", 1);

        Assert.Equal(AddResult.NoSpace, inv.TryAdd("potion", 1));
        Assert.Equal(AddResult.NoSpace, inv.TryAdd("axe", 1));
        Assert.Equal(10, inv.CountOf("potion"));
    }

    [Fact]
    public void TryAdd_MoreThanFits_IsAllOrNothing()
    {
        var inv = TestItems.Inventory(2);
        inv.TryAdd("potion", 5);
        Assert.Equal(AddResult.NoSpace, inv.TryAdd("potion", 16)); // room is 5 + 10 = 15
        Assert.Equal(5, inv.CountOf("potion"));
        Assert.Equal(AddResult.Added, inv.TryAdd("potion", 15));
    }

    [Fact]
    public void TryAdd_UniqueItems_GetDistinctInstanceIds()
    {
        var inv = TestItems.Inventory(3);
        inv.TryAdd("sword", 2);
        Assert.NotEqual(inv.Get(0)!.InstanceId, inv.Get(1)!.InstanceId);
        Assert.NotNull(inv.Get(0)!.InstanceId);
    }

    [Fact]
    public void TryAdd_BadInput_IsRejected()
    {
        var inv = TestItems.Inventory(1);
        Assert.Equal(AddResult.InvalidCount, inv.TryAdd("potion", 0));
        Assert.Equal(AddResult.UnknownItem, inv.TryAdd("nothing", 1));
    }

    [Fact]
    public void TryRemove_MoreThanOwned_ChangesNothing()
    {
        var inv = TestItems.Inventory(2);
        inv.TryAdd("potion", 12);
        Assert.False(inv.TryRemove("potion", 13));
        Assert.True(inv.TryRemove("potion", 3));
        Assert.Equal(9, inv.CountOf("potion"));
        Assert.Null(inv.Get(1));
    }

    [Fact]
    public void TryEquip_WhenSlotOccupied_SwapsIntoTheFreedInventorySlot()
    {
        var inv = TestItems.Inventory(2);
        var eq = new Equipment(TestItems.Catalog());
        inv.TryAdd("sword", 1);
        inv.TryAdd("axe", 1);

        Assert.True(eq.TryEquip(inv, 0));
        Assert.Equal("sword", eq.Get(EquipSlot.Weapon)!.ItemId);
        Assert.Null(inv.Get(0));

        Assert.True(eq.TryEquip(inv, 1));
        Assert.Equal("axe", eq.Get(EquipSlot.Weapon)!.ItemId);
        Assert.Equal("sword", inv.Get(1)!.ItemId);
    }

    [Fact]
    public void TryEquip_NonEquipment_IsRefused()
    {
        var inv = TestItems.Inventory(1);
        inv.TryAdd("potion", 1);
        Assert.False(new Equipment(TestItems.Catalog()).TryEquip(inv, 0));
    }
}
