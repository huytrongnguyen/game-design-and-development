namespace Course.Scripting;

public class QuestCatalogTests
{
    private static readonly MapProperties Hero = new(new Dictionary<string, double> { ["STR"] = 12, ["LV"] = 5 });

    private static HookRegistry NewRegistry()
    {
        var registry = new HookRegistry();
        registry.Register("QUEST_DONE_WOLVES", ctx =>
        {
            ctx.GrantItem("wolf_pelt", 2);
            ctx.GrantGold(100);
        });
        registry.Register("QUEST_DONE_NOTHING", _ => { });
        return registry;
    }

    [Fact]
    public void Load_MissingHook_FailsAtLoadNamingQuestAndHook()
    {
        var defs = new[] { new QuestDef("wolves", "QUEST_DONE_WOLFES") };

        var ex = Assert.Throws<ContentValidationException>(() => QuestCatalog.Load(defs, NewRegistry(), Hero.Names));

        Assert.Single(ex.Problems);
        Assert.Equal("Quest 'wolves': hook 'QUEST_DONE_WOLFES' is not registered.", ex.Problems[0]);
    }

    [Fact]
    public void Load_SeveralProblems_ReportsAllAtOnce()
    {
        var defs = new[]
        {
            new QuestDef("a", "QUEST_DONE_MISSING"),
            new QuestDef("b", "QUEST_DONE_NOTHING", "STR * "),
            new QuestDef("c", "QUEST_DONE_NOTHING", "LUCK * 2"),
        };

        var ex = Assert.Throws<ContentValidationException>(() => QuestCatalog.Load(defs, NewRegistry(), Hero.Names));

        Assert.Equal(3, ex.Problems.Count);
        Assert.Contains("unknown property 'LUCK'", ex.Problems[2]);
    }

    [Fact]
    public void TryComplete_QuestWithHook_GrantsFormulaGoldThenHookRewards()
    {
        var catalog = QuestCatalog.Load(
            new[] { new QuestDef("wolves", "QUEST_DONE_WOLVES", "STR * 2 + LV") },
            NewRegistry(), Hero.Names);
        var player = new PlayerState();

        var done = catalog.TryComplete("wolves", player, Hero);

        Assert.True(done);
        Assert.Equal(129, player.Gold);            // 29 from the formula + 100 from the hook
        Assert.Equal(2, player.ItemCount("wolf_pelt"));
    }

    [Fact]
    public void TryComplete_UnknownQuest_ReturnsFalseAndChangesNothing()
    {
        var catalog = QuestCatalog.Load(Array.Empty<QuestDef>(), NewRegistry(), Hero.Names);
        var player = new PlayerState();

        Assert.False(catalog.TryComplete("nope", player, Hero));
        Assert.Equal(0, player.Gold);
    }

    [Fact]
    public void TryComplete_RunTwiceOnFreshPlayers_GivesIdenticalOutcome()
    {
        var catalog = QuestCatalog.Load(
            new[] { new QuestDef("wolves", "QUEST_DONE_WOLVES", "STR * 2 + LV") },
            NewRegistry(), Hero.Names);
        var a = new PlayerState();
        var b = new PlayerState();

        catalog.TryComplete("wolves", a, Hero);
        catalog.TryComplete("wolves", b, Hero);

        Assert.Equal(a.Gold, b.Gold);
        Assert.Equal(a.ItemCount("wolf_pelt"), b.ItemCount("wolf_pelt"));
    }

    [Fact]
    public void GrantGold_NegativeAmount_Throws()
    {
        var context = new HookContext(new PlayerState(), Hero);
        Assert.Throws<ArgumentOutOfRangeException>(() => context.GrantGold(-1));
    }
}
