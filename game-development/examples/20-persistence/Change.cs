namespace Course.Persistence;

/// <summary>One edit to one player: a gold delta and/or an item-count delta.</summary>
public readonly record struct Change(string PlayerId, long GoldDelta = 0, string? ItemId = null, int ItemDelta = 0);
