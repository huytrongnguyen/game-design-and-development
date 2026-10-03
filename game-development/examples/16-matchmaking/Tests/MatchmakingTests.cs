namespace Course.Matchmaking.Tests;

public class MatchmakingTests
{
    // 1 tank + 1 healer + 2 damage; bracket 2, +2 per 100 ticks, max 10; cross-shard after 300; 600-tick strike lockout.
    private static readonly QueueRules Rules = SampleRules.Dungeon;

    private static QueueEntry E(string id, Role role, int level, long tick = 0, string shard = "A") =>
        new(id, role, level, shard, tick);

    private static Matchmaker Make(int slots = 5) => new(Rules, new InstanceAllocator(slots));

    private static void Group(Matchmaker m, int level = 10, long tick = 0, string prefix = "")
    {
        m.Enqueue(E(prefix + "tank", Role.Tank, level, tick));
        m.Enqueue(E(prefix + "heal", Role.Healer, level, tick));
        m.Enqueue(E(prefix + "dps1", Role.Damage, level, tick));
        m.Enqueue(E(prefix + "dps2", Role.Damage, level + 1, tick));
    }

    [Fact]
    public void Tick_OneOfEachRoleAndTwoDamage_FormsOnePartyWithAnInstance()
    {
        var m = Make();
        Group(m);
        var formed = m.Tick(0);
        var party = Assert.Single(formed);
        Assert.Equal(4, party.Members.Count);
        Assert.Equal("inst-1", party.InstanceId);
        Assert.Equal(0, m.QueueLength);
    }

    [Fact]
    public void Tick_FourDamageDealersAndNoHealer_NeverForms()
    {
        var m = Make();
        for (var i = 1; i <= 4; i++) m.Enqueue(E($"d{i}", Role.Damage, 10));
        m.Enqueue(E("tank", Role.Tank, 10));
        Assert.Empty(m.Tick(0));
        Assert.Empty(m.Tick(5000));
        Assert.Equal(5, m.QueueLength);
    }

    [Fact]
    public void Tick_LevelGapOfFive_FormsOnlyOnceTheBracketHasWidenedTo6()
    {
        var m = Make();
        m.Enqueue(E("a-tank", Role.Tank, 10));
        m.Enqueue(E("heal", Role.Healer, 15));
        m.Enqueue(E("dps1", Role.Damage, 15));
        m.Enqueue(E("dps2", Role.Damage, 15));
        Assert.Equal(2, Rules.BracketAt(99));
        Assert.Equal(4, Rules.BracketAt(100));
        Assert.Equal(6, Rules.BracketAt(200));
        Assert.Empty(m.Tick(99));
        Assert.Empty(m.Tick(100));   // bracket 4 < gap 5
        Assert.Single(m.Tick(200));  // bracket 6 >= gap 5
    }

    [Fact]
    public void BracketAt_LongWait_IsCappedAtTheMaximum()
    {
        Assert.Equal(10, Rules.BracketAt(100_000));
        Assert.Equal(10, Rules.BracketAt(400)); // 2 + 4*2 = 10 exactly
    }

    [Fact]
    public void Tick_TwoTanksWaiting_TheOneWhoWaitedLongerGetsTheParty()
    {
        var m = Make();
        m.Enqueue(E("tank-late", Role.Tank, 10, tick: 20));
        m.Enqueue(E("tank-early", Role.Tank, 10, tick: 5));
        m.Enqueue(E("heal", Role.Healer, 10, tick: 20));
        m.Enqueue(E("dps1", Role.Damage, 10, tick: 20));
        m.Enqueue(E("dps2", Role.Damage, 10, tick: 20));
        var party = Assert.Single(m.Tick(20));
        Assert.Contains(party.Members, x => x.PlayerId == "tank-early");
        Assert.DoesNotContain(party.Members, x => x.PlayerId == "tank-late");
        Assert.Equal(1, m.QueueLength);
    }

    [Fact]
    public void Tick_OtherShardDamageDealer_IsMixedInOnlyAfter300Ticks()
    {
        var m = Make();
        m.Enqueue(E("tank", Role.Tank, 10));
        m.Enqueue(E("heal", Role.Healer, 10));
        m.Enqueue(E("dps1", Role.Damage, 10));
        m.Enqueue(E("dps2", Role.Damage, 10, shard: "B"));
        Assert.Empty(m.Tick(299));
        Assert.Single(m.Tick(300));
    }

    [Fact]
    public void Tick_OnlyOneSlot_SecondPartyWaitsThenGetsTheFreedSlot()
    {
        var m = Make(slots: 1);
        Group(m, prefix: "x-");
        Group(m, prefix: "y-");
        var formed = m.Tick(0);
        Assert.Equal(2, formed.Count);
        Assert.Equal("inst-1", formed[0].InstanceId);
        Assert.Null(formed[1].InstanceId);

        m.Finish(formed[0]);
        m.Tick(1);
        Assert.Equal("inst-1", formed[1].InstanceId);
    }

    [Fact]
    public void Leave_AfterStart_GivesAStrikeAndALockoutOf600Ticks()
    {
        var m = Make();
        Group(m);
        m.Tick(0);
        m.Leave("dps2", now: 50);
        Assert.Equal(1, m.Leavers.Strikes("dps2"));
        Assert.Equal(EnqueueResult.LockedOut, m.Enqueue(E("dps2", Role.Damage, 11, tick: 649)));
        Assert.Equal(EnqueueResult.Queued, m.Enqueue(E("dps2", Role.Damage, 11, tick: 650)));
    }

    [Fact]
    public void Leave_SecondStrike_DoublesTheLockout()
    {
        var m = Make();
        m.Leavers.Strike("p", 0);
        m.Leavers.Strike("p", 1000);
        Assert.False(m.Leavers.CanQueue("p", 2199));
        Assert.True(m.Leavers.CanQueue("p", 2200)); // 1000 + 2 * 600
    }

    [Fact]
    public void Tick_AfterALeaver_AQueuedDamageDealerBackfillsTheOpenSlot()
    {
        var m = Make();
        Group(m);
        var party = m.Tick(0)[0];
        m.Leave("dps2", now: 50);
        Assert.Equal(1, m.OpenBackfills);
        Assert.Equal(3, party.Members.Count);

        m.Enqueue(E("newcomer", Role.Damage, 11, tick: 60));
        var formedNew = m.Tick(60);
        Assert.Empty(formedNew);
        Assert.Equal(4, party.Members.Count);
        Assert.Contains(party.Members, x => x.PlayerId == "newcomer");
        Assert.Equal(0, m.OpenBackfills);
        Assert.Equal(0, m.QueueLength);
    }

    [Fact]
    public void Tick_BackfillCandidateTenLevelsAway_IsAcceptedAtWait400NotAt399()
    {
        var m = Make();
        Group(m);              // average level of the three who remain is 10
        var party = m.Tick(0)[0];
        m.Leave("dps2", now: 50);
        m.Enqueue(E("far", Role.Damage, 20, tick: 60));
        m.Tick(449);           // wait 399 -> bracket 8
        Assert.Equal(3, party.Members.Count);
        m.Tick(450);           // wait 400 -> bracket 10
        Assert.Equal(4, party.Members.Count);
    }

    [Fact]
    public void Tick_BackfillNeedsTheMissingRole_ADamageDealerDoesNotFillAHealerSlot()
    {
        var m = Make();
        Group(m);
        var party = m.Tick(0)[0];
        m.Leave("heal", now: 10);
        m.Enqueue(E("dps3", Role.Damage, 10, tick: 11));
        m.Tick(11);
        Assert.Equal(3, party.Members.Count);
        Assert.Equal(1, m.OpenBackfills);
    }

    [Fact]
    public void Leave_BeforeTheInstanceIsAllocated_NoStrikeAndTheOthersReturnToTheQueue()
    {
        var m = Make(slots: 0);
        Group(m);
        var party = m.Tick(0)[0];
        Assert.Null(party.InstanceId);
        m.Leave("heal", now: 5);
        Assert.Equal(0, m.Leavers.Strikes("heal"));
        Assert.Equal(3, m.QueueLength);
        Assert.Empty(party.Members);
    }

    [Fact]
    public void Leave_EveryMember_ReleasesTheInstance()
    {
        var alloc = new InstanceAllocator(1);
        var m = new Matchmaker(Rules, alloc);
        Group(m);
        m.Tick(0);
        Assert.Equal(0, alloc.FreeCount);
        foreach (var id in new[] { "tank", "heal", "dps1", "dps2" }) m.Leave(id, 30);
        Assert.Equal(1, alloc.FreeCount);
        Assert.Equal(0, m.OpenBackfills);
        Assert.Equal(4, m.Leavers.Strikes("tank") + m.Leavers.Strikes("heal") + m.Leavers.Strikes("dps1") + m.Leavers.Strikes("dps2"));
    }

    [Fact]
    public void Enqueue_SamePlayerTwice_IsRejected()
    {
        var m = Make();
        Assert.Equal(EnqueueResult.Queued, m.Enqueue(E("p", Role.Tank, 5)));
        Assert.Equal(EnqueueResult.AlreadyQueued, m.Enqueue(E("p", Role.Tank, 5)));
        Assert.True(m.Cancel("p"));
        Assert.Equal(EnqueueResult.Queued, m.Enqueue(E("p", Role.Tank, 5)));
    }
}
