namespace Course.Sync;

public class SyncServerTests
{
    private static readonly EntityId Viewer = new(1);

    private static SyncServer NewServer(SyncConfig? config = null)
    {
        var s = new SyncServer(config ?? new SyncConfig());
        s.Spawn(1, 0, 0);
        s.AddViewer(1);
        return s;
    }

    [Fact]
    public void Replicate_EntityEntersSight_ExactlyOneEnterWithFullState()
    {
        var s = NewServer();
        s.Spawn(2, 30, 40, hp: 77, appearance: 5);   // distance 50, inside sight 100

        var msgs = s.Replicate().For(Viewer);

        var enter = Assert.IsType<EnterMessage>(Assert.Single(msgs));
        Assert.Equal(new EnterMessage(new EntityId(2), 30, 40, 77, 5), enter);
    }

    [Fact]
    public void Replicate_EntityBeyondSight_NothingSent()
    {
        var s = NewServer();
        s.Spawn(2, 101, 0);

        Assert.Empty(s.Replicate().For(Viewer));
    }

    [Fact]
    public void Replicate_UnchangedEntity_ProducesNoUpdate()
    {
        var s = NewServer();
        s.Spawn(2, 10, 0);
        s.Replicate();

        Assert.Empty(s.Replicate().For(Viewer));
        Assert.Empty(s.Replicate().For(Viewer));
    }

    [Fact]
    public void Replicate_SettingSameValue_IsNotAChange()
    {
        var s = NewServer();
        s.Spawn(2, 10, 0);
        s.Replicate();

        s.Move(2, 10, 0);
        s.SetHp(2, 100);

        Assert.Empty(s.Replicate().For(Viewer));
    }

    [Fact]
    public void Replicate_MovingEntity_UpdateCarriesOnlyPosition()
    {
        var s = NewServer();
        s.Spawn(2, 10, 0);
        s.Replicate();

        s.Move(2, 12, 0);
        var update = Assert.IsType<UpdateMessage>(Assert.Single(s.Replicate().For(Viewer)));

        Assert.Equal(FieldMask.Position, update.Mask);
        Assert.Equal(12, update.X);
        Assert.Equal(1 + 4 + 1 + 8, WireSize.Of(update));   // 14 bytes, not the 21 of a full Enter
    }

    [Fact]
    public void Replicate_HpChange_UpdateCarriesOnlyHp()
    {
        var s = NewServer();
        s.Spawn(2, 10, 0);
        s.Replicate();

        s.SetHp(2, 60);
        var update = Assert.IsType<UpdateMessage>(Assert.Single(s.Replicate().For(Viewer)));

        Assert.Equal(FieldMask.Hp, update.Mask);
        Assert.Equal(60, update.Hp);
        Assert.Equal(1 + 4 + 1 + 4, WireSize.Of(update));
    }

    [Fact]
    public void Replicate_EntityWalksOutOfSight_ExactlyOneLeave()
    {
        var s = NewServer();
        s.Spawn(2, 50, 0);
        s.Replicate();

        s.Move(2, 500, 0);
        var msgs = s.Replicate().For(Viewer);
        Assert.Equal(new LeaveMessage(new EntityId(2)), Assert.Single(msgs));

        s.Move(2, 501, 0);
        Assert.Empty(s.Replicate().For(Viewer));
        Assert.Equal(0, s.KnownCount(1));
    }

    [Fact]
    public void Replicate_EntityHoversBetweenSightAndLeaveRange_StaysKnown()
    {
        var s = NewServer();                       // sight 100, leave 120
        s.Spawn(2, 99, 0);
        s.Replicate();

        s.Move(2, 110, 0);                         // outside sight, inside leave ring
        var msgs = s.Replicate().For(Viewer);
        Assert.IsType<UpdateMessage>(Assert.Single(msgs));   // no Leave, no flicker

        s.Move(2, 130, 0);
        Assert.IsType<LeaveMessage>(Assert.Single(s.Replicate().For(Viewer)));
    }

    [Fact]
    public void Replicate_DespawnedEntity_ProducesLeave()
    {
        var s = NewServer();
        s.Spawn(2, 10, 0);
        s.Replicate();

        s.Despawn(2);

        Assert.IsType<LeaveMessage>(Assert.Single(s.Replicate().For(Viewer)));
    }

    [Fact]
    public void Replicate_HundredIdleEntities_BytesAreZeroAfterFirstTick()
    {
        var s = NewServer();
        for (var i = 0; i < 100; i++) s.Spawn(100 + i, i % 10 * 5, i / 10 * 5);

        var first = s.Replicate();
        var second = s.Replicate();
        var third = s.Replicate();

        Assert.Equal(100 * 21, first.TotalBytes);   // 2100 bytes: 100 full Enters
        Assert.Equal(0, second.TotalBytes);
        Assert.Equal(0, third.TotalBytes);
    }

    [Fact]
    public void Replicate_FarEntityMovingEveryTick_UpdatedEveryFourthTickWithLatestPosition()
    {
        var s = NewServer();                       // near range 50, far interval 4
        s.Spawn(4, 80, 0);                         // far; (tick + 4) % 4 == 0 when tick % 4 == 0
        s.Replicate();                             // tick 1: Enter

        var updateTicks = new List<long>();
        float lastX = 0;
        for (var i = 1; i <= 8; i++)
        {
            s.Move(4, 80 + i, 0);
            var r = s.Replicate();
            foreach (var m in r.For(Viewer))
                if (m is UpdateMessage u) { updateTicks.Add(r.Tick); lastX = u.X; }
        }

        Assert.Equal(new long[] { 4, 8 }, updateTicks);
        Assert.Equal(87, lastX);                   // coalesced: it jumped, it did not replay each step
    }

    [Fact]
    public void Replicate_NearEntityMovingEveryTick_UpdatedEveryTick()
    {
        var s = NewServer();
        s.Spawn(2, 10, 0);
        s.Replicate();

        var updates = 0;
        for (var i = 1; i <= 8; i++)
        {
            s.Move(2, 10 + i, 0);
            updates += s.Replicate().For(Viewer).Count;
        }

        Assert.Equal(8, updates);
    }

    [Fact]
    public void Replicate_BudgetTooSmall_DefersNearestFirstAndLosesNothing()
    {
        var s = NewServer(new SyncConfig(BudgetBytesPerViewerTick: 42));   // room for two 21-byte Enters
        s.Spawn(2, 10, 0);
        s.Spawn(3, 20, 0);
        s.Spawn(4, 30, 0);

        var t1 = s.Replicate();
        Assert.Equal(new[] { 2, 3 }, t1.For(Viewer).Select(m => m.Id.Value));
        Assert.Equal(42, t1.TotalBytes);

        var t2 = s.Replicate();
        Assert.Equal(new[] { 4 }, t2.For(Viewer).Select(m => m.Id.Value));
        Assert.Empty(s.Replicate().For(Viewer));
    }

    [Fact]
    public void Replicate_TwoViewers_EachGetsItsOwnView()
    {
        var s = NewServer();
        s.Spawn(2, 1000, 0);
        s.AddViewer(2);
        s.Spawn(3, 1010, 0);

        var r = s.Replicate();

        Assert.Empty(r.For(new EntityId(1)));                       // nothing near viewer 1
        Assert.Equal(new[] { 3 }, r.For(new EntityId(2)).Select(m => m.Id.Value));
    }

    [Fact]
    public void ClientWorld_FollowingAllMessages_MatchesServerState()
    {
        var s = NewServer();
        var client = new ClientWorld();
        s.Spawn(2, 10, 0);
        s.Spawn(3, 60, 0);
        for (var i = 0; i < 12; i++)
        {
            s.Move(2, 10 + i * 3, i);
            if (i == 5) s.SetHp(3, 10);
            if (i == 9) s.Move(3, 900, 0);                           // walks away
            client.ApplyAll(s.Replicate(), Viewer);
        }

        Assert.True(client.Entities.ContainsKey(new EntityId(2)));
        Assert.False(client.Entities.ContainsKey(new EntityId(3)));
        var two = client.Entities[new EntityId(2)];
        Assert.Equal((43f, 11f), (two.X, two.Y));
    }

    [Fact]
    public void Entity_IsDirtyAt_OnlyDuringTheTickOfTheChange()
    {
        var e = new Entity(new EntityId(9), 0, 0, 10, 0, tick: 1);

        e.SetHp(5, tick: 3);

        Assert.True(e.IsDirtyAt(3));
        Assert.False(e.IsDirtyAt(4));
        Assert.Equal(FieldMask.Hp, e.ChangedSince(2));
        Assert.Equal(FieldMask.None, e.ChangedSince(3));
    }
}
