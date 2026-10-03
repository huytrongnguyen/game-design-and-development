namespace Course.Economy;

/// <summary>Settles one sale as a single ledger event: buyer pays the full price, seller gets it minus the fee, the fee is destroyed.</summary>
public static class TradeSettlement
{
    public const string Sink = "system:sink";

    public static ApplyStatus Settle(Ledger ledger, string tradeId, string buyer, string seller, long price, MarketFees fees, string currency = "gold")
    {
        var fee = fees.SaleFee(price);
        return ledger.TryApply($"trade:{tradeId}", "market sale",
        [
            new LedgerLine(buyer, currency, -price),
            new LedgerLine(seller, currency, price - fee),
            new LedgerLine(Sink, currency, fee),
        ]);
    }
}
