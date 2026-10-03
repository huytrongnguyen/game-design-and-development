namespace Course.Quests.Tests;

public class MissionTests
{
    // 4-player limit, 100-tick clock, 100 points per kill, S at 500, A at 300.
    private static readonly MissionDef Cave = new("mission.cave", "Normal", 1, 4, 2, 100, 100, 500, 300);

    private static PartyMember[] Party(int size, int level = 5) =>
        Enumerable.Range(1, size).Select(i => new PartyMember($"p{i}", level)).ToArray();

    [Fact]
    public void TryEnter_FourMembersWhenLimitIsFour_Enters()
    {
        var m = new MissionInstance(Cave);
        Assert.Equal(EnterResult.Entered, m.TryEnter(Party(4), nowTick: 10));
        Assert.Equal(MissionInstance.Phase.Running, m.State);
        Assert.Equal(110, m.DeadlineTick);
    }

    [Fact]
    public void TryEnter_FiveMembersWhenLimitIsFour_IsRejectedWithoutSideEffects()
    {
        var m = new MissionInstance(Cave);
        Assert.Equal(EnterResult.PartyTooLarge, m.TryEnter(Party(5), 10));
        Assert.Equal(MissionInstance.Phase.Open, m.State);
        Assert.Empty(m.Members);
        Assert.Equal(EnterResult.Entered, m.TryEnter(Party(4), 10)); // still enterable
    }

    [Fact]
    public void TryEnter_MemberBelowMinLevel_IsRejected()
    {
        var m = new MissionInstance(Cave);
        Assert.Equal(EnterResult.LevelTooLow, m.TryEnter([new("p1", 5), new("p2", 1)], 0));
    }

    [Fact]
    public void TryEnter_AfterTheRunStarted_IsRejected()
    {
        var m = new MissionInstance(Cave);
        m.TryEnter(Party(2), 0);
        Assert.Equal(EnterResult.NotOpen, m.TryEnter(Party(2), 1));
    }

    [Fact]
    public void Tick_ReachingDeadline_EndsOnTheExactTick()
    {
        var manager = new MissionManager();
        var m = manager.Create(Cave);
        m.TryEnter(Party(4), nowTick: 10); // deadline 110

        Assert.Empty(manager.Tick(109));
        Assert.Equal(MissionInstance.Phase.Running, m.State);

        var results = manager.Tick(110);
        Assert.Equal(MissionOutcome.TimedOut, Assert.Single(results).Outcome);
    }

    [Fact]
    public void Tick_AfterTimeout_InstanceIsDestroyedAndRemoved()
    {
        var manager = new MissionManager();
        var m = manager.Create(Cave);
        m.TryEnter(Party(4), 10);

        manager.Tick(110);

        Assert.Equal(0, manager.LiveCount);
        Assert.Equal(MissionInstance.Phase.Destroyed, m.State);
        Assert.Empty(m.Members);
        Assert.Equal(EnterResult.NotOpen, m.TryEnter(Party(1), 111));
    }

    [Fact]
    public void Complete_ClearedWithTimeLeft_ScoresKillsPlusTimeBonus()
    {
        var m = new MissionInstance(Cave);
        m.TryEnter(Party(4), 0); // deadline 100
        for (var i = 0; i < 4; i++) m.RecordKill();

        var result = m.Complete(nowTick: 50)!.Value; // 4*100 + (100-50)/10 = 405

        Assert.Equal(405, result.Score);
        Assert.Equal('A', result.Grade);
        Assert.Equal(MissionOutcome.Cleared, result.Outcome);
    }

    [Fact]
    public void Tick_ClearedRunIsDestroyedOnTheNextManagerTick()
    {
        var manager = new MissionManager();
        var m = manager.Create(Cave);
        m.TryEnter(Party(2), 0);
        m.Complete(20);

        Assert.Empty(manager.Tick(21)); // result was already returned by Complete
        Assert.Equal(0, manager.LiveCount);
        Assert.Equal(MissionInstance.Phase.Destroyed, m.State);
    }

    [Fact]
    public void Complete_AfterTimeout_IsIgnored()
    {
        var m = new MissionInstance(Cave);
        m.TryEnter(Party(1), 0);
        m.Tick(100);
        Assert.Null(m.Complete(101));
        Assert.Equal(MissionOutcome.TimedOut, m.Outcome);
    }

    [Fact]
    public void Complete_ClearEventFeedsTheQuestLog()
    {
        QuestCatalog.TryLoad(SampleQuests.Json, out var catalog, out _);
        var log = new QuestLog(catalog!, 5);
        log.TryAccept("q.wolves");
        log.Apply(new GameEvent(ObjectiveKind.Kill, "wolf", 3));
        log.Apply(new GameEvent(ObjectiveKind.Talk, "elder"));
        log.TryClaim("q.wolves");
        log.TryAccept("q.cave");

        var m = new MissionInstance(Cave);
        m.TryEnter(Party(4), 0);
        if (m.Complete(10) is { Outcome: MissionOutcome.Cleared } r)
            log.Apply(new GameEvent(ObjectiveKind.Clear, r.MissionId));

        Assert.Equal(1, log.ProgressOf("q.cave", "clear"));
    }
}
