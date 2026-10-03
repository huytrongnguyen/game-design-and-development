namespace Course.ActionCombat;

public class ShapeTests
{
    private static readonly ShapeDef Cone = new() { Kind = ShapeKind.Cone, Range = 5, HalfAngleDeg = 40 };

    private static Vec2 At(double dist, double deg) => Vec2.FromAngle(deg) * dist;

    [Fact]
    public void Cone_Hits_At_30_Degrees_And_Misses_At_50()
    {
        Assert.True(ShapeOverlap.Hits(Cone, new(0, 0), 0, At(4, 30), 0.3));
        Assert.False(ShapeOverlap.Hits(Cone, new(0, 0), 0, At(4, 50), 0.3));
    }

    [Fact]
    public void Cone_Misses_Beyond_Range_And_Behind()
    {
        Assert.False(ShapeOverlap.Hits(Cone, new(0, 0), 0, At(6, 0), 0.3));
        Assert.False(ShapeOverlap.Hits(Cone, new(0, 0), 0, At(2, 180), 0.3));
    }

    [Fact]
    public void Cone_Follows_Facing()
    {
        Assert.True(ShapeOverlap.Hits(Cone, new(0, 0), 90, At(3, 100), 0.3));
        Assert.False(ShapeOverlap.Hits(Cone, new(0, 0), 90, At(3, 0), 0.3));
    }

    [Fact]
    public void Rectangle_Uses_Length_And_Half_Width()
    {
        var rect = new ShapeDef { Kind = ShapeKind.Rectangle, Length = 4, Width = 2 };
        Assert.True(ShapeOverlap.Hits(rect, new(0, 0), 0, new(3, 0.9), 0.1));
        Assert.False(ShapeOverlap.Hits(rect, new(0, 0), 0, new(3, 1.5), 0.1));
        Assert.False(ShapeOverlap.Hits(rect, new(0, 0), 0, new(4.5, 0), 0.1));
        Assert.True(ShapeOverlap.Hits(rect, new(0, 0), 0, new(4.4, 0), 0.5)); // the target's size reaches in
    }

    [Fact]
    public void Circle_And_Capsule()
    {
        var circle = new ShapeDef { Kind = ShapeKind.Circle, Radius = 2, Offset = 1 };
        Assert.True(ShapeOverlap.Hits(circle, new(0, 0), 0, new(3, 0), 0.1));   // centre at x=1, reach 2.1
        Assert.False(ShapeOverlap.Hits(circle, new(0, 0), 0, new(-2, 0), 0.1));
        var cap = new ShapeDef { Kind = ShapeKind.Capsule, Length = 3, Radius = 0.5 };
        Assert.True(ShapeOverlap.Hits(cap, new(0, 0), 0, new(2, 0.5), 0.1));
        Assert.True(ShapeOverlap.Hits(cap, new(0, 0), 0, new(3.5, 0), 0.1));    // rounded end
        Assert.False(ShapeOverlap.Hits(cap, new(0, 0), 0, new(3.7, 0.4), 0.1));
    }
}

public class TimelineTests
{
    private static readonly ShapeDef Rect = new() { Kind = ShapeKind.Rectangle, Length = 3, Width = 2 };

    private static SkillDef Slash(params int[] hitTicks) => new()
    {
        Id = "slash", StartupTicks = 3, ActiveTicks = 2, RecoveryTicks = 5,
        HitTicks = hitTicks, Shape = Rect, Damage = 10, CancelFromTick = 5, CancelToTick = 8,
    };

    private static (ActionWorld w, Combatant a, Combatant t) Arena()
    {
        var w = new ActionWorld(new ActionRules());
        var a = w.Add("a", 1, new(0, 0), 0.5, 100);
        var t = w.Add("t", 2, new(2, 0), 0.5, 100);
        return (w, a, t);
    }

    [Fact]
    public void Hit_Lands_Only_On_The_Listed_Tick()
    {
        var (w, a, t) = Arena();
        w.TryStart(a, Slash(3));
        for (int i = 0; i < 12; i++) w.Step();
        var hit = Assert.Single(w.Events);
        Assert.Equal(3, hit.Tick);
        Assert.Equal(90, t.Hp);
    }

    [Fact]
    public void Multi_Hit_Skill_Hits_On_Each_Active_Tick()
    {
        var (w, a, t) = Arena();
        w.TryStart(a, Slash(3, 4));
        for (int i = 0; i < 12; i++) w.Step();
        Assert.Equal(new[] { 3, 4 }, w.Events.Select(e => e.Tick).ToArray());
        Assert.Equal(80, t.Hp);
    }

    [Fact]
    public void Nothing_Hits_During_Startup_Or_Recovery()
    {
        var (w, a, t) = Arena();
        w.TryStart(a, Slash(3));
        for (int i = 0; i < 3; i++) w.Step();   // ticks 0..2: startup
        Assert.Equal(100, t.Hp);
    }

    [Fact]
    public void Cancel_Window_Allows_The_Next_Skill_Only_From_Tick_5()
    {
        var (w, a, _) = Arena();
        w.TryStart(a, Slash(3));
        for (int i = 0; i < 4; i++) w.Step();
        Assert.False(w.TryStart(a, Slash(3)));  // offset 4: still committed
        w.Step();
        Assert.True(w.TryStart(a, Slash(3)));   // offset 5: cancel window opens
    }

    [Fact]
    public void Skill_Moves_The_Caster_During_Its_Segment()
    {
        var (w, a, _) = Arena();
        var lunge = new SkillDef
        {
            Id = "lunge", StartupTicks = 4, RecoveryTicks = 2,
            Moves = [new MoveSegment(1, 3, 0.5)],   // ticks 1 and 2, half a unit each
        };
        w.TryStart(a, lunge);
        for (int i = 0; i < 6; i++) w.Step();
        Assert.Equal(1.0, a.Position.X, 6);
    }

    [Fact]
    public void Overlapping_Bodies_Are_Pushed_Apart()
    {
        var w = new ActionWorld(new ActionRules());
        var a = w.Add("a", 1, new(0, 0), 0.5, 10);
        var b = w.Add("b", 2, new(0.5, 0), 0.5, 10);
        w.Step();
        Assert.Equal(-0.25, a.Position.X, 6);
        Assert.Equal(0.75, b.Position.X, 6);
        Assert.Equal(1.0, (b.Position - a.Position).Length, 6);
    }
}

public class DodgeTests
{
    private static readonly SkillDef Poke = new()
    {
        Id = "poke", ActiveTicks = 1, RecoveryTicks = 2, HitTicks = [0], Damage = 10,
        Shape = new() { Kind = ShapeKind.Rectangle, Length = 3, Width = 2 },
    };
    private static readonly SkillDef Dodge = new()
    {
        Id = "dodge", RecoveryTicks = 6, IFrameFromTick = 0, IFrameToTick = 3,
    };

    private static (ActionWorld w, Combatant a, Combatant t) Arena()
    {
        var w = new ActionWorld(new ActionRules());
        return (w, w.Add("a", 1, new(0, 0), 0.5, 100), w.Add("t", 2, new(2, 0), 0.5, 100));
    }

    [Fact]
    public void Without_A_Dodge_The_Poke_Lands()
    {
        var (w, a, t) = Arena();
        w.TryStart(a, Poke);
        w.Step();
        Assert.Equal(90, t.Hp);
    }

    [Fact]
    public void IFrames_Cancel_The_Hit()
    {
        var (w, a, t) = Arena();
        w.TryStart(t, Dodge);
        w.TryStart(a, Poke);
        w.Step();
        Assert.Equal(100, t.Hp);
        Assert.Equal(HitOutcome.Dodged, Assert.Single(w.Events).Outcome);
    }

    [Fact]
    public void A_Hit_After_The_IFrames_Lands()
    {
        var (w, a, t) = Arena();
        w.TryStart(t, Dodge);
        for (int i = 0; i < 3; i++) w.Step();   // i-frames cover offsets 0, 1, 2
        w.TryStart(a, Poke);                    // hit test at tick 3: offset 3, no longer invulnerable
        w.Step();
        Assert.Equal(90, t.Hp);
    }
}

public class LagCompensationTests
{
    private static readonly SkillDef LongPoke = new()
    {
        Id = "long-poke", ActiveTicks = 1, HitTicks = [0], Damage = 10,
        Shape = new() { Kind = ShapeKind.Rectangle, Length = 8, Width = 2 },
    };

    // The target runs along +X at one unit per tick: x equals the tick number.
    private static (ActionWorld w, Combatant a, Combatant t) RunTarget(int lagTicks)
    {
        var w = new ActionWorld(new ActionRules());
        var a = w.Add("a", 1, new(0, 0), 0.5, 100);
        var t = w.Add("t", 2, new(0, 0), 0.5, 100);
        for (int tick = 0; tick < 10; tick++) { w.SetPosition(t, new(tick, 0)); w.Step(); }
        w.SetPosition(t, new(10, 0));
        a.ViewLagTicks = lagTicks;
        w.TryStart(a, LongPoke);
        w.Step();                               // resolves at tick 10
        return (w, a, t);
    }

    [Fact]
    public void Without_Lag_Compensation_The_Shot_Misses_A_Moving_Target()
        => Assert.Equal(100, RunTarget(0).t.Hp);        // the target is at x = 10, beyond the reach of 8

    [Fact]
    public void Rewind_Of_3_Ticks_Uses_The_Old_Position()
        => Assert.Equal(90, RunTarget(3).t.Hp);         // at tick 7 the target was at x = 7

    [Fact]
    public void Rewind_Is_Clamped_To_The_Maximum()
    {
        var rules = new ActionRules();
        // 40 ms one-way + 100 ms interpolation at 20 Hz (50 ms per tick) = 140 ms -> 3 ticks.
        Assert.Equal(3, LagCompensation.RewindTicks(40, 100, rules));
        // 300 ms + 100 ms = 8 ticks, clamped to 6.
        Assert.Equal(6, LagCompensation.RewindTicks(300, 100, rules));
        Assert.Equal(5, LagCompensation.MsToTicks(250, rules));
    }

    [Fact]
    public void History_Keeps_Only_The_Last_Capacity_Ticks()
    {
        var h = new PositionHistory(32);
        for (int t = 0; t < 40; t++) h.Record(t, new(t, 0));
        Assert.True(h.TryGet(8, out var p));
        Assert.Equal(8, p.X);
        Assert.False(h.TryGet(7, out _));
        Assert.False(h.TryGet(100, out _));
    }
}

public class DataTests
{
    private const string Json = """
    [
      { "id": "slash", "startupTicks": 3, "activeTicks": 2, "recoveryTicks": 5, "hitTicks": [3],
        "damage": 10, "cancelFromTick": 5, "cancelToTick": 8,
        "shape": { "kind": "Cone", "range": 4, "halfAngleDeg": 40 } },
      { "id": "roll", "recoveryTicks": 8, "iFrameFromTick": 1, "iFrameToTick": 5,
        "moves": [ { "fromTick": 0, "toTick": 4, "distancePerTick": 0.75 } ] }
    ]
    """;

    [Fact]
    public void Skills_Load_From_Json()
    {
        var skills = SkillLoader.Load(Json);
        Assert.Equal(10, skills["slash"].TotalTicks);
        Assert.Equal(ShapeKind.Cone, skills["slash"].Shape!.Kind);
        Assert.True(skills["roll"].IsInvulnerableAt(1));
        Assert.False(skills["roll"].IsInvulnerableAt(5));
        Assert.Equal(0.75, skills["roll"].Moves[0].DistancePerTick);
    }

    [Fact]
    public void A_Hit_Tick_Outside_The_Active_Window_Is_Rejected()
    {
        const string bad = """
            [ { "id": "bad", "startupTicks": 3, "activeTicks": 2, "hitTicks": [5],
                "shape": { "kind": "Circle", "radius": 1 } } ]
            """;
        var ex = Assert.Throws<InvalidDataException>(() => SkillLoader.Load(bad));
        Assert.Contains("hit tick 5", ex.Message);
    }

    [Fact]
    public void Changing_Only_The_Data_Changes_The_Outcome()
    {
        // The same target at 30 degrees is hit by a 40-degree cone and missed by a 20-degree one.
        var wide = SkillLoader.Load(Json)["slash"];
        var narrow = wide with { Shape = wide.Shape! with { HalfAngleDeg = 20 } };
        foreach (var (skill, expected) in new[] { (wide, 90), (narrow, 100) })
        {
            var w = new ActionWorld(new ActionRules());
            var a = w.Add("a", 1, new(0, 0), 0.5, 100);
            var t = w.Add("t", 2, Vec2.FromAngle(30) * 3, 0.3, 100);
            w.TryStart(a, skill);
            for (int i = 0; i < 6; i++) w.Step();
            Assert.Equal(expected, t.Hp);
        }
    }
}
