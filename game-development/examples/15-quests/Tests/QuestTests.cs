namespace Course.Quests.Tests;

public class QuestTests
{
    private static QuestCatalog Load()
    {
        Assert.True(QuestCatalog.TryLoad(SampleQuests.Json, out var catalog, out var errors), string.Join("; ", errors));
        return catalog!;
    }

    private static QuestLog FinishWolves(QuestLog log)
    {
        log.TryAccept("q.wolves");
        log.Apply(new GameEvent(ObjectiveKind.Kill, "wolf", 3));
        log.Apply(new GameEvent(ObjectiveKind.Talk, "elder"));
        return log;
    }

    [Fact]
    public void Constructor_ChainWithPrerequisite_OnlyRootIsAvailable()
    {
        var log = new QuestLog(Load(), level: 5);
        Assert.Equal(QuestState.Available, log.StateOf("q.wolves"));
        Assert.Equal(QuestState.Locked, log.StateOf("q.cave"));
    }

    [Fact]
    public void Claim_PrerequisiteCompletedButNotRewarded_FollowUpStaysLocked()
    {
        var log = FinishWolves(new QuestLog(Load(), level: 5));
        Assert.Equal(QuestState.Completed, log.StateOf("q.wolves"));
        Assert.Equal(QuestState.Locked, log.StateOf("q.cave"));

        log.TryClaim("q.wolves");
        Assert.Equal(QuestState.Available, log.StateOf("q.cave"));
    }

    [Fact]
    public void Refresh_PrerequisiteRewardedButLevelTooLow_FollowUpUnlocksOnLevelUp()
    {
        var log = FinishWolves(new QuestLog(Load(), level: 1));
        log.TryClaim("q.wolves");
        Assert.Equal(QuestState.Locked, log.StateOf("q.cave")); // needs level 2

        log.SetLevel(2);
        Assert.Equal(QuestState.Available, log.StateOf("q.cave"));
    }

    [Fact]
    public void Apply_KillEvents_AdvanceOnlyTheMatchingObjective()
    {
        var log = new QuestLog(Load(), level: 5);
        log.TryAccept("q.wolves");

        log.Apply(new GameEvent(ObjectiveKind.Kill, "boar", 5));  // wrong target
        log.Apply(new GameEvent(ObjectiveKind.Collect, "wolf", 5)); // wrong kind
        log.Apply(new GameEvent(ObjectiveKind.Kill, "wolf", 2));

        Assert.Equal(2, log.ProgressOf("q.wolves", "kill"));
        Assert.Equal(0, log.ProgressOf("q.wolves", "talk"));
        Assert.Equal(QuestState.Active, log.StateOf("q.wolves"));
    }

    [Fact]
    public void Apply_ExtraKills_CountIsCappedAtTheObjectiveTotal()
    {
        var log = new QuestLog(Load(), level: 5);
        log.TryAccept("q.wolves");
        log.Apply(new GameEvent(ObjectiveKind.Kill, "wolf", 10));
        Assert.Equal(3, log.ProgressOf("q.wolves", "kill"));
    }

    [Fact]
    public void Apply_AllObjectivesDone_QuestCompletesAndCutsceneIsTriggered()
    {
        var log = new QuestLog(Load(), level: 5);
        log.TryAccept("q.wolves");
        log.Apply(new GameEvent(ObjectiveKind.Kill, "wolf", 3));
        Assert.Equal(QuestState.Active, log.StateOf("q.wolves")); // talk still missing

        log.Apply(new GameEvent(ObjectiveKind.Talk, "elder"));
        Assert.Equal(QuestState.Completed, log.StateOf("q.wolves"));
        Assert.Equal(["dialogue:elder.intro", "cutscene:gate.relief"], log.DrainTriggers());
        Assert.Empty(log.DrainTriggers());
    }

    [Fact]
    public void Apply_EventsForAnInactiveQuest_AreIgnored()
    {
        var log = new QuestLog(Load(), level: 5); // wolves available but not accepted
        log.Apply(new GameEvent(ObjectiveKind.Kill, "wolf", 3));
        Assert.Equal(0, log.ProgressOf("q.wolves", "kill"));
    }

    [Fact]
    public void TryClaim_CalledTwice_PaysTheRewardOnce()
    {
        var log = FinishWolves(new QuestLog(Load(), level: 5));
        var first = log.TryClaim("q.wolves");
        Assert.Equal(200, first!.Exp);
        Assert.Null(log.TryClaim("q.wolves"));
    }

    [Fact]
    public void TryClaim_FinalQuest_NamesTheUnlockedCharacter()
    {
        var log = FinishWolves(new QuestLog(Load(), level: 5));
        log.TryClaim("q.wolves");
        log.TryAccept("q.cave");
        log.Apply(new GameEvent(ObjectiveKind.Reach, "cave"));
        log.Apply(new GameEvent(ObjectiveKind.Clear, "mission.cave"));
        Assert.Equal("fellow.rina", log.TryClaim("q.cave")!.UnlockCharacter);
    }

    [Fact]
    public void TryLoad_UnknownPrerequisite_ReportsTheBrokenLink()
    {
        const string json = """[{"id":"a","title":"A","prerequisites":["ghost"],"objectives":[{"id":"o","kind":"Talk","target":"x"}],"reward":{}}]""";
        Assert.False(QuestCatalog.TryLoad(json, out _, out var errors));
        Assert.Contains("quest 'a' requires unknown quest 'ghost'", errors);
    }

    [Fact]
    public void TryLoad_PrerequisiteCycle_IsRejected()
    {
        const string json = """
        [{"id":"a","title":"A","prerequisites":["b"],"objectives":[{"id":"o","kind":"Talk","target":"x"}],"reward":{}},
         {"id":"b","title":"B","prerequisites":["a"],"objectives":[{"id":"o","kind":"Talk","target":"x"}],"reward":{}}]
        """;
        Assert.False(QuestCatalog.TryLoad(json, out _, out var errors));
        Assert.Contains(errors, e => e.StartsWith("prerequisite cycle"));
    }
}
