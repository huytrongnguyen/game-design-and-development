namespace M12.Ai.Tests;

public class MonsterTests
{
    private static MonsterConfig Cfg => new() { ThinkIntervalTicks = 5 };

    [Fact]
    public void Tick_UnitInsideAggroRadius_StartsChasing()
    {
        var m = new Monster(1, new Vec2(0, 0), Cfg);
        var u = new Unit(100, new Vec2(9.9, 0), 100);
        m.Tick(0, [u]);
        Assert.Equal(MonsterState.Chase, m.State);
        Assert.Equal(20, m.Hate.ThreatOf(100));
    }

    [Fact]
    public void Tick_UnitOutsideAggroRadius_StaysIdle()
    {
        var m = new Monster(1, new Vec2(0, 0), Cfg);
        var u = new Unit(100, new Vec2(10.1, 0), 100);
        m.Tick(0, [u]);
        Assert.Equal(MonsterState.Idle, m.State);
        Assert.Equal(0, m.Hate.Count);
    }

    [Fact]
    public void Tick_ChaseReachesTarget_AttacksOnCooldown()
    {
        var m = new Monster(1, new Vec2(0, 0), Cfg);
        var u = new Unit(100, new Vec2(5, 0), 100);
        for (long t = 0; t < 20; t++) m.Tick(t, [u]);
        Assert.Equal(MonsterState.Attack, m.State);
        // First attack on the think at tick 5 (monster at x=3 after 3 moves, range 2 -> in range at tick 5), then cooldown 10.
        Assert.Equal(100 - 5, u.Hp);
    }

    [Fact]
    public void Tick_AnotherAttackerPassesThreshold_MonsterSwitchesTarget()
    {
        var m = new Monster(1, new Vec2(0, 0), Cfg);
        var a = new Unit(100, new Vec2(1, 0), 1000);
        var b = new Unit(101, new Vec2(1, 1), 1000);
        m.Tick(0, [a, b]); // nearest (a) gets 20 initial hate
        m.Tick(5, [a, b]);
        Assert.Equal(100, m.TargetId);
        m.OnDamaged(101, 22); // a's 20 has decayed to about 19.2; 22 beats 19.2 * 1.1
        m.Tick(10, [a, b]);
        Assert.Equal(101, m.TargetId);
    }

    [Fact]
    public void Tick_TargetKitedPastLeash_ReturnsHomeClearsHateAndResets()
    {
        var m = new Monster(1, new Vec2(0, 0), Cfg with { LeashDistance = 30 });
        var u = new Unit(100, new Vec2(8, 0), 100);
        m.OnDamaged(100, 40); // hurt it first so the reset is visible
        long t = 0;
        while (m.State != MonsterState.Return && t < 200)
        {
            u.Position = new Vec2(m.Position.X + 8, 0);
            m.Tick(t++, [u]);
        }
        Assert.Equal(MonsterState.Return, m.State);
        Assert.Equal(0, m.Hate.Count);
        // Tick 0 only picks the target; it moves from tick 5, so x = tick - 5 and the
        // first think with x > 30 is tick 40 (x = 35).
        Assert.Equal(40, t - 1);

        for (var i = 0; i < 100 && m.State == MonsterState.Return; i++) m.Tick(t++, [u]);
        Assert.Equal(MonsterState.Idle, m.State);
        Assert.Equal(new Vec2(0, 0), m.Position);
        Assert.Equal(100, m.Hp);
    }

    [Fact]
    public void OnDamaged_WhileReturning_IsIgnored()
    {
        var m = new Monster(1, new Vec2(0, 0), Cfg with { LeashDistance = 1 });
        var u = new Unit(100, new Vec2(5, 0), 100);
        for (long t = 0; t < 30 && m.State != MonsterState.Return; t++) m.Tick(t, [u]);
        Assert.Equal(MonsterState.Return, m.State);
        m.OnDamaged(100, 50);
        Assert.Equal(0, m.Hate.Count);
        Assert.Equal(100, m.Hp);
    }
}
