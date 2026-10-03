namespace Course.Loot;

/// <summary>Item power and gear score: how the slots, rarity and upgrade level add up to one number.</summary>
public static class Gear
{
    public static int StatBudget(LootData d, int itemLevel) =>
        (int)Math.Round(d.Budget.Base * (1 + d.Budget.GrowthPerLevel * (itemLevel - 1)));

    /// <summary>slot weight x stat budget x rarity power x (1 + bonus per upgrade level), rounded to a whole number.</summary>
    public static int ItemPower(LootData d, string slot, int itemLevel, string rarity, int upgrade)
    {
        double w = d.Slots.Single(s => s.Name == slot).Weight;
        return (int)Math.Round(w * StatBudget(d, itemLevel) * d.Rarity(rarity).Power * (1 + d.Upgrade.PerLevelBonus * upgrade));
    }

    /// <summary>Gear score of a full set: the sum of the item powers of every slot.</summary>
    public static int GearScore(LootData d, int itemLevel, string rarity, int upgrade) =>
        d.Slots.Sum(s => ItemPower(d, s.Name, itemLevel, rarity, upgrade));
}
