namespace Course.Persistence;

public class TradeSagaTests
{
    private sealed class Crash : Exception { }

    private static readonly TradeOrder Order = new("t1", "seller", "buyer", "sword", 1, 100);

    private static InMemoryGameStore Setup(long buyerGold)
    {
        var s = new InMemoryGameStore();
        s.CreatePlayer(PlayerRecord.New("seller", gold: 5));
        s.CreatePlayer(PlayerRecord.New("buyer", gold: buyerGold));
        s.Apply("seed", "seed", [new("seller", ItemId: "sword", ItemDelta: 1)]);
        return s;
    }

    private static (long Gold, int Swords) Totals(InMemoryGameStore s)
    {
        var all = new[] { "seller", "buyer", TradeSaga.EscrowId("t1") }
            .Select(s.Find).Where(p => p is not null).Select(p => p!).ToList();
        return (all.Sum(p => p.Gold), all.Sum(p => p.Count("sword")));
    }

    [Fact]
    public void Run_BuyerCanPay_ItemAndGoldSwap()
    {
        var s = Setup(buyerGold: 150);

        var status = new TradeSaga(s).Run(Order);

        Assert.Equal(SagaStatus.Completed, status);
        Assert.Equal((105L, 0), (s.Find("seller")!.Gold, s.Find("seller")!.Count("sword")));
        Assert.Equal((50L, 1), (s.Find("buyer")!.Gold, s.Find("buyer")!.Count("sword")));
        Assert.Equal(0, s.Find(TradeSaga.EscrowId("t1"))!.Count("sword"));
    }

    [Fact]
    public void Run_BuyerCannotPay_StepTwoRejectedAndItemReturnedToSeller()
    {
        var s = Setup(buyerGold: 99);

        var status = new TradeSaga(s).Run(Order);

        Assert.Equal(SagaStatus.Compensated, status);
        Assert.Equal(1, s.Find("seller")!.Count("sword"));
        Assert.Equal(99, s.Find("buyer")!.Gold);
        Assert.Equal(5, s.Find("seller")!.Gold);
        Assert.Equal(["trade.escrow", "trade.refund"], s.Log.Where(e => e.OpId != "seed").Select(e => e.Kind));
    }

    [Theory]
    [InlineData("escrow-applied")]
    [InlineData("escrow-saved")]
    [InlineData("settle-applied")]
    public void Recover_CrashAtAnyStepWithRichBuyer_CompletesExactlyOnce(string crashAt)
    {
        var s = Setup(buyerGold: 150);
        var crashing = new TradeSaga(s, step => { if (step == crashAt) throw new Crash(); });
        Assert.Throws<Crash>(() => crashing.Run(Order));

        var status = new TradeSaga(s).Recover("t1");          // the restarted server

        Assert.Equal(SagaStatus.Completed, status);
        Assert.Equal(105, s.Find("seller")!.Gold);
        Assert.Equal(50, s.Find("buyer")!.Gold);
        Assert.Equal(1, s.Find("buyer")!.Count("sword"));
        Assert.Equal((155L, 1), Totals(s));                    // nothing created, nothing lost
    }

    [Theory]
    [InlineData("escrow-applied")]
    [InlineData("escrow-saved")]
    [InlineData("compensating-saved")]
    [InlineData("refund-applied")]
    public void Recover_CrashAtAnyStepWithPoorBuyer_CompensatesExactlyOnce(string crashAt)
    {
        var s = Setup(buyerGold: 10);
        var crashing = new TradeSaga(s, step => { if (step == crashAt) throw new Crash(); });
        Assert.Throws<Crash>(() => crashing.Run(Order));

        var status = new TradeSaga(s).Recover("t1");

        Assert.Equal(SagaStatus.Compensated, status);
        Assert.Equal(1, s.Find("seller")!.Count("sword"));
        Assert.Equal(0, s.Find(TradeSaga.EscrowId("t1"))!.Count("sword"));
        Assert.Equal((15L, 1), Totals(s));
        Assert.Equal(1, s.Log.Count(e => e.Kind == "trade.escrow"));
        Assert.Equal(1, s.Log.Count(e => e.Kind == "trade.refund"));
    }

    [Fact]
    public void Run_SellerLacksTheItem_FailsWithoutTouchingAnything()
    {
        var s = Setup(buyerGold: 500);
        var order = Order with { Item = "shield" };

        var status = new TradeSaga(s).Run(order);

        Assert.Equal(SagaStatus.Failed, status);
        Assert.Equal(500, s.Find("buyer")!.Gold);
        Assert.Equal(5, s.Find("seller")!.Gold);
    }

    [Fact]
    public void Recover_AlreadyCompleted_DoesNothingMore()
    {
        var s = Setup(buyerGold: 150);
        var saga = new TradeSaga(s);
        saga.Run(Order);
        var logSize = s.Log.Count;

        Assert.Equal(SagaStatus.Completed, saga.Recover("t1"));
        Assert.Equal(logSize, s.Log.Count);
    }
}
