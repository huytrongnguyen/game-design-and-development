namespace Course.Economy;

/// <summary>
/// One independent drop chance of "1 in <see cref="Denominator"/>". A denominator of 1 is a
/// guaranteed drop. This is the denominator style, where designers write 1000 and mean 1 in 1000.
/// </summary>
public sealed record DropEntry(string ItemId, int Denominator, int Quantity = 1);
