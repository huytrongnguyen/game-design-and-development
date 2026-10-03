namespace Course.MiniEngine;

public class MiniEngineTests
{
    [Fact]
    public void Step_TwoLinearMovers_ExactPositionsAfterThreeTicks()
    {
        var store = new EntityStore();
        var ids = DataLoader.LoadEntities(DemoScenario.Json, store);
        var engine = new Engine(store, new EventQueue(), [new MovementSystem()]);

        for (var i = 0; i < 3; i++)
            engine.Step();

        Assert.True(store.TryGet<Position>(ids[0], out var moverA));
        Assert.Equal(new Position(3, 0), moverA);

        Assert.True(store.TryGet<Position>(ids[1], out var moverB));
        Assert.Equal(new Position(10, -1), moverB);
    }

    [Fact]
    public void Step_TimerEntity_FiresEventOnceOnTickThreeThenNeverAgain()
    {
        var store = new EntityStore();
        var ids = DataLoader.LoadEntities(DemoScenario.Json, store);
        var engine = new Engine(store, new EventQueue(), [new TimerSystem()]);

        var tick1 = engine.Step();
        var tick2 = engine.Step();
        var tick3 = engine.Step();
        var tick4 = engine.Step();

        Assert.Empty(tick1);
        Assert.Empty(tick2);
        Assert.Equal(new GameEvent("Bell", 3, ids[2]), Assert.Single(tick3));
        Assert.Empty(tick4);
        Assert.False(store.Has<Timer>(ids[2]));
    }

    [Fact]
    public void Step_RandomWalkerSameSeed_ReplaysTheExactSamePath()
    {
        static Position[] RunWithSeed(int seed)
        {
            var store = new EntityStore();
            var ids = DataLoader.LoadEntities(DemoScenario.Json, store);
            var engine = new Engine(store, new EventQueue(), [new RandomWalkSystem(new Random(seed))]);

            var path = new Position[5];
            for (var i = 0; i < path.Length; i++)
            {
                engine.Step();
                store.TryGet<Position>(ids[3], out path[i]);
            }

            return path;
        }

        var replayA = RunWithSeed(42);
        var replayB = RunWithSeed(42);
        var differentSeed = RunWithSeed(7);

        Assert.Equal(replayA, replayB);
        Assert.NotEqual(replayA, differentSeed);
    }

    [Fact]
    public void Step_FullDemoScenario_RunsAllSystemsInFixedOrderEachTick()
    {
        var store = new EntityStore();
        var ids = DataLoader.LoadEntities(DemoScenario.Json, store);
        var engine = new Engine(
            store,
            new EventQueue(),
            [new MovementSystem(), new TimerSystem(), new RandomWalkSystem(new Random(1))]);

        IReadOnlyList<GameEvent> lastTickEvents = [];
        for (var i = 0; i < 3; i++)
            lastTickEvents = engine.Step();

        Assert.True(store.TryGet<Position>(ids[0], out var moverA));
        Assert.Equal(new Position(3, 0), moverA);

        Assert.True(store.TryGet<Position>(ids[1], out var moverB));
        Assert.Equal(new Position(10, -1), moverB);

        Assert.Equal(new GameEvent("Bell", 3, ids[2]), Assert.Single(lastTickEvents));
        Assert.Equal(3L, engine.Tick);
    }
}
