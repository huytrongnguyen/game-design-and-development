namespace M12.Ai.Tests;

public class ThinkRateTests
{
    [Fact]
    public void Tick_100TicksInterval5_ThinksExactly20Times()
    {
        var m = new Monster(1, new Vec2(0, 0), new MonsterConfig { ThinkIntervalTicks = 5 });
        for (long t = 0; t < 100; t++) m.Tick(t, []);
        Assert.Equal(20, m.ThinkCount);
    }

    [Fact]
    public void Tick_UnitAppearsBetweenThinks_IsNoticedOnNextThinkTick()
    {
        var m = new Monster(1, new Vec2(0, 0), new MonsterConfig { ThinkIntervalTicks = 5 });
        var u = new Unit(100, new Vec2(5, 0), 100);
        m.Tick(0, []);
        for (long t = 1; t < 5; t++)
        {
            m.Tick(t, [u]);
            Assert.Equal(MonsterState.Idle, m.State);
        }
        m.Tick(5, [u]);
        Assert.Equal(MonsterState.Chase, m.State);
    }

    [Fact]
    public void Tick_TenMonstersWithStaggeredOffsets_ThinkTwicePerTick()
    {
        var cfg = new MonsterConfig { ThinkIntervalTicks = 5 };
        var ms = Enumerable.Range(0, 10).Select(i => new Monster(i, new Vec2(0, 0), cfg, thinkOffset: i % 5)).ToList();
        for (long t = 0; t < 10; t++)
        {
            var before = ms.Sum(m => m.ThinkCount);
            foreach (var m in ms) m.Tick(t, []);
            Assert.Equal(2, ms.Sum(m => m.ThinkCount) - before);
        }
    }
}
