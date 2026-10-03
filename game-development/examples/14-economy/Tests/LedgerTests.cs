namespace Course.Economy.Tests;

public class LedgerTests
{
    private static LedgerLine[] Grant(string who, long amount) =>
        [new("system:faucet", "gold", -amount), new(who, "gold", amount)];

    [Fact]
    public void TryApply_SameKeyTwice_SecondIsANoOp()
    {
        var ledger = new Ledger();
        Assert.Equal(ApplyStatus.Applied, ledger.TryApply("quest:7:reward", "quest", Grant("alice", 500)));
        Assert.Equal(ApplyStatus.Duplicate, ledger.TryApply("quest:7:reward", "quest", Grant("alice", 500)));
        Assert.Equal(500, ledger.Balance("alice", "gold"));
        Assert.Equal(2, ledger.Entries.Count);
    }

    [Fact]
    public void Balance_EqualsSumOfLedgerEntries()
    {
        var ledger = new Ledger();
        ledger.TryApply("a", "grant", Grant("alice", 1000));
        ledger.TryApply("b", "grant", Grant("bob", 300));
        ledger.TryApply("c", "pay", [new("alice", "gold", -250), new("bob", "gold", 250)]);
        ledger.TryApply("a", "grant", Grant("alice", 1000)); // retry, ignored

        foreach (var who in new[] { "alice", "bob", "system:faucet" })
            Assert.Equal(ledger.BalanceFromEntries(who, "gold"), ledger.Balance(who, "gold"));
        Assert.Equal(750, ledger.Balance("alice", "gold"));
        Assert.Equal(550, ledger.Balance("bob", "gold"));
    }

    [Fact]
    public void TryApply_Overdraft_RejectsTheWholeGroupAndLeavesNoTrace()
    {
        var ledger = new Ledger();
        ledger.TryApply("g", "grant", Grant("alice", 100));
        var status = ledger.TryApply("p", "pay", [new("alice", "gold", -101), new("bob", "gold", 101)]);
        Assert.Equal(ApplyStatus.InsufficientFunds, status);
        Assert.Equal(0, ledger.Balance("bob", "gold"));
        Assert.Equal(2, ledger.Entries.Count);
        // the key was not consumed, so a corrected retry works
        Assert.Equal(ApplyStatus.Applied, ledger.TryApply("p", "pay", [new("alice", "gold", -100), new("bob", "gold", 100)]));
    }

    [Fact]
    public void TryApply_TotalSupplyAcrossAllAccounts_StaysZero()
    {
        var ledger = new Ledger();
        ledger.TryApply("g", "grant", Grant("alice", 10_000));
        TradeSettlement.Settle(ledger, "t1", "alice", "bob", 10_000, MarketFees.Sample);
        var total = ledger.Entries.Sum(e => e.Delta);
        Assert.Equal(0, total);
        Assert.Equal(10_000, -ledger.Balance("system:faucet", "gold")); // faucet: total created
        Assert.Equal(200, ledger.Balance(TradeSettlement.Sink, "gold")); // sink: total destroyed
    }

    [Fact]
    public void TryApply_ItemsUseTheSameMechanism()
    {
        var ledger = new Ledger();
        ledger.TryApply("drop:1", "drop", [new("system:world", "item:potion", -3), new("alice", "item:potion", 3)]);
        Assert.Equal(ApplyStatus.InsufficientFunds, ledger.TryApply("use:1", "use", [new("alice", "item:potion", -4)]));
        Assert.Equal(ApplyStatus.Applied, ledger.TryApply("use:2", "use", [new("alice", "item:potion", -3)]));
    }

    [Fact]
    public void TryApply_EmptyOrZeroLines_AreInvalid()
    {
        var ledger = new Ledger();
        Assert.Equal(ApplyStatus.Invalid, ledger.TryApply("x", "r", []));
        Assert.Equal(ApplyStatus.Invalid, ledger.TryApply("x", "r", [new("alice", "gold", 0)]));
    }
}

public class TradeSettlementTests
{
    [Fact]
    public void Settle_10000Sale_SellerNets9800AndFeeIsDestroyed()
    {
        var ledger = new Ledger();
        ledger.TryApply("g", "grant", [new("system:faucet", "gold", -10_000), new("buyer", "gold", 10_000)]);
        Assert.Equal(ApplyStatus.Applied, TradeSettlement.Settle(ledger, "t1", "buyer", "seller", 10_000, MarketFees.Sample));
        Assert.Equal(0, ledger.Balance("buyer", "gold"));
        Assert.Equal(9_800, ledger.Balance("seller", "gold"));
        Assert.Equal(200, ledger.Balance(TradeSettlement.Sink, "gold"));
        Assert.Equal(50, MarketFees.Sample.ListingFee(10_000));
    }

    [Fact]
    public void Settle_SameTradeTwice_PaysOnce()
    {
        var ledger = new Ledger();
        ledger.TryApply("g", "grant", [new("system:faucet", "gold", -20_000), new("buyer", "gold", 20_000)]);
        TradeSettlement.Settle(ledger, "t1", "buyer", "seller", 10_000, MarketFees.Sample);
        Assert.Equal(ApplyStatus.Duplicate, TradeSettlement.Settle(ledger, "t1", "buyer", "seller", 10_000, MarketFees.Sample));
        Assert.Equal(10_000, ledger.Balance("buyer", "gold"));
    }
}
