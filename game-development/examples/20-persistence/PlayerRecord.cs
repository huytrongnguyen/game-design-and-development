using System.Collections.Immutable;

namespace Course.Persistence;

/// <summary>The durable, valuable part of a player: gold and items. Immutable, so a
/// transaction can stage new versions without touching the committed ones.</summary>
public sealed record PlayerRecord(string Id, long Gold, ImmutableDictionary<string, int> Items)
{
    public static PlayerRecord New(string id, long gold = 0) =>
        new(id, gold, ImmutableDictionary<string, int>.Empty);

    public int Count(string item) => Items.GetValueOrDefault(item);

    public PlayerRecord Apply(Change c)
    {
        var items = Items;
        if (c.ItemId is not null && c.ItemDelta != 0)
        {
            var n = Count(c.ItemId) + c.ItemDelta;
            items = n == 0 ? items.Remove(c.ItemId) : items.SetItem(c.ItemId, n);
        }
        return this with { Gold = Gold + c.GoldDelta, Items = items };
    }
}
