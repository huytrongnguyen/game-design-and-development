namespace Course.Economy;

/// <summary>
/// The fee shape of a player market: a small non-refundable listing fee, and a larger
/// commission taken from the seller's proceeds when the item sells. Both destroy currency: they are sinks.
/// Rates are data, in thousandths.
/// </summary>
public sealed record MarketFees(int ListingPermille, int SalePermille)
{
    /// <summary>Sample rates: 0.5% listing fee, 2% sale commission.</summary>
    public static MarketFees Sample { get; } = new(5, 20);

    /// <summary>Paid up front on the total listing value.</summary>
    public long ListingFee(long totalPrice) => totalPrice * ListingPermille / 1000;

    /// <summary>Taken from the seller when the item sells.</summary>
    public long SaleFee(long totalPrice) => totalPrice * SalePermille / 1000;
}
