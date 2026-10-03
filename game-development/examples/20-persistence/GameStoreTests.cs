namespace Course.Persistence;

public class GameStoreTests
{
    private static InMemoryGameStore NewStore()
    {
        var s = new InMemoryGameStore();
        s.CreatePlayer(PlayerRecord.New("alice", gold: 100));
        s.CreatePlayer(PlayerRecord.New("bob", gold: 50));
        return s;
    }

    private sealed class Crash : Exception { }

    [Fact]
    public void Apply_ValidTransfer_AppliesAllChangesAndLogsOnce()
    {
        var s = NewStore();

        var r = s.Apply("op1", "pay", [new("alice", GoldDelta: -30), new("bob", GoldDelta: 30)]);

        Assert.Equal(ApplyOutcome.Applied, r.Outcome);
        Assert.Equal(70, s.Find("alice")!.Gold);
        Assert.Equal(80, s.Find("bob")!.Gold);
        var entry = Assert.Single(s.Log);
        Assert.Equal(("op1", "pay", 1L), (entry.OpId, entry.Kind, entry.Seq));
    }

    [Fact]
    public void Apply_SecondChangeInvalid_NothingApplied()
    {
        var s = NewStore();

        // Bob would pay 60 but only has 50: the whole operation, including Alice's +60, is refused.
        var r = s.Apply("op1", "pay", [new("alice", GoldDelta: 60), new("bob", GoldDelta: -60)]);

        Assert.Equal(ApplyOutcome.Rejected, r.Outcome);
        Assert.Equal(100, s.Find("alice")!.Gold);
        Assert.Equal(50, s.Find("bob")!.Gold);
        Assert.Empty(s.Log);
    }

    [Fact]
    public void Apply_CrashBetweenChanges_LeavesNoPartialStateAndRetrySucceeds()
    {
        var s = NewStore();
        s.BeforeCommit = i => { if (i == 1) throw new Crash(); };   // dies after staging change 0

        Assert.Throws<Crash>(() => s.Apply("op1", "pay", [new("alice", GoldDelta: -30), new("bob", GoldDelta: 30)]));

        Assert.Equal(100, s.Find("alice")!.Gold);
        Assert.Equal(50, s.Find("bob")!.Gold);
        Assert.Empty(s.Log);

        s.BeforeCommit = null;                                      // "restart"
        var retry = s.Apply("op1", "pay", [new("alice", GoldDelta: -30), new("bob", GoldDelta: 30)]);
        Assert.Equal(ApplyOutcome.Applied, retry.Outcome);
        Assert.Equal(70, s.Find("alice")!.Gold);
    }

    [Fact]
    public void Apply_SameOperationIdTwice_AppliesOnce()
    {
        var s = NewStore();
        Change[] pay = [new("alice", GoldDelta: -30), new("bob", GoldDelta: 30)];

        var first = s.Apply("op1", "pay", pay);
        var second = s.Apply("op1", "pay", pay);   // a client retry after a lost reply

        Assert.Equal(ApplyOutcome.Applied, first.Outcome);
        Assert.Equal(ApplyOutcome.Duplicate, second.Outcome);
        Assert.True(second.Ok);
        Assert.Equal(70, s.Find("alice")!.Gold);   // not 40
        Assert.Single(s.Log);
    }

    [Fact]
    public void Apply_RejectedOperation_CanBeRetriedLaterWithSameId()
    {
        var s = NewStore();
        Change[] pay = [new("bob", GoldDelta: -80), new("alice", GoldDelta: 80)];

        Assert.Equal(ApplyOutcome.Rejected, s.Apply("op1", "pay", pay).Outcome);
        s.Apply("income", "reward", [new("bob", GoldDelta: 40)]);

        Assert.Equal(ApplyOutcome.Applied, s.Apply("op1", "pay", pay).Outcome);
        Assert.Equal(10, s.Find("bob")!.Gold);
    }

    [Fact]
    public void Apply_ItemChanges_CountsAndRemovesEmptyStacks()
    {
        var s = NewStore();
        s.Apply("a", "drop", [new("alice", ItemId: "potion", ItemDelta: 3)]);
        Assert.Equal(3, s.Find("alice")!.Count("potion"));

        var tooMany = s.Apply("b", "use", [new("alice", ItemId: "potion", ItemDelta: -4)]);
        Assert.Equal(ApplyOutcome.Rejected, tooMany.Outcome);

        s.Apply("c", "use", [new("alice", ItemId: "potion", ItemDelta: -3)]);
        Assert.Empty(s.Find("alice")!.Items);
    }

    [Fact]
    public void Apply_UnknownPlayer_Rejected()
    {
        var s = NewStore();
        var r = s.Apply("x", "pay", [new("nobody", GoldDelta: 5)]);
        Assert.Equal(ApplyOutcome.Rejected, r.Outcome);
    }

    [Fact]
    public void Log_ReturnedList_IsACopyThatCannotChangeTheStore()
    {
        var s = NewStore();
        s.Apply("a", "reward", [new("alice", GoldDelta: 1)]);
        var copy = (IList<LogEntry>)s.Log.ToList();
        copy.Clear();
        Assert.Single(s.Log);
    }
}
