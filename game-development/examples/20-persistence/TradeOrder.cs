namespace Course.Persistence;

/// <summary>The seller hands over items, the buyer pays gold.</summary>
public sealed record TradeOrder(string OrderId, string Seller, string Buyer, string Item, int Quantity, long Price);
