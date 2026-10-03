namespace Course.Persistence;

public class CheckpointTests
{
    [Fact]
    public void Tick_DirtyPlayer_FiresOnIntervalBoundaries()
    {
        var sch = new CheckpointScheduler(intervalTicks: 100);
        sch.Login("a", tick: 0);
        sch.MarkDirty("a");

        Assert.Empty(sch.Tick(99));
        var cp = Assert.Single(sch.Tick(100));
        Assert.Equal(new Checkpoint("a", 100, CheckpointReason.Interval), cp);

        Assert.Empty(sch.Tick(200));        // clean since the last save: no write
        sch.MarkDirty("a");
        Assert.Empty(sch.Tick(199));    // 99 ticks after the save at 100
        Assert.Single(sch.Tick(200));
    }

    [Fact]
    public void Tick_PlayersLoggedInAtDifferentTicks_SaveAtDifferentTicks()
    {
        var sch = new CheckpointScheduler(100);
        sch.Login("a", 0);
        sch.Login("b", 40);
        sch.MarkDirty("a"); sch.MarkDirty("b");

        Assert.Equal(["a"], sch.Tick(100).Select(c => c.PlayerId));
        Assert.Equal(["b"], sch.Tick(140).Select(c => c.PlayerId));
    }

    [Fact]
    public void Logout_DirtyPlayer_CheckpointsImmediately()
    {
        var sch = new CheckpointScheduler(100);
        sch.Login("a", 0);
        sch.MarkDirty("a");

        var cp = sch.Logout("a", tick: 7);

        Assert.Equal(new Checkpoint("a", 7, CheckpointReason.Logout), cp);
        Assert.Empty(sch.Tick(1000));       // no longer online
    }

    [Fact]
    public void Logout_CleanPlayer_NoCheckpoint()
    {
        var sch = new CheckpointScheduler(100);
        sch.Login("a", 0);
        Assert.Null(sch.Logout("a", 7));
    }

    [Fact]
    public void ZoneChangeAndImportantEvent_AlwaysCheckpoint()
    {
        var sch = new CheckpointScheduler(100);
        sch.Login("a", 0);

        Assert.Equal(CheckpointReason.ZoneChange, sch.ZoneChanged("a", 5)!.Value.Reason);
        Assert.Equal(CheckpointReason.ImportantEvent, sch.ImportantEvent("a", 6)!.Value.Reason);
    }

    [Fact]
    public void WriteBehind_ManyUpdatesInOneInterval_BecomeOneDatabaseWrite()
    {
        var store = new InMemoryGameStore();
        var cache = new WriteBehindCache(store, intervalTicks: 100);
        cache.Login("a", 0);

        for (var t = 1; t <= 99; t++)
        {
            cache.Update(cache.Get("a") with { X = t });
            cache.Tick(t);
        }
        Assert.Equal(0, store.SnapshotWrites);

        cache.Tick(100);

        Assert.Equal(1, store.SnapshotWrites);
        Assert.Equal(99, store.LoadSnapshot("a")!.X);
    }

    [Fact]
    public void WriteBehind_CrashAfterCheckpoint_LosesOnlyTheUnsavedInterval()
    {
        var store = new InMemoryGameStore();
        var cache = new WriteBehindCache(store, 100);
        cache.Login("a", 0);
        cache.Update(cache.Get("a") with { Exp = 500 });
        cache.Tick(100);                                    // saved: exp 500
        cache.Update(cache.Get("a") with { Exp = 800 });    // never saved

        var afterRestart = new WriteBehindCache(store, 100).Login("a", 150);

        Assert.Equal(500, afterRestart.Exp);
    }

    [Fact]
    public void WriteBehind_ZoneChangeImportantEventAndLogout_SaveAtOnce()
    {
        var store = new InMemoryGameStore();
        var cache = new WriteBehindCache(store, 1000);
        cache.Login("a", 0);

        cache.ChangeZone("a", "orpesia", 3);
        Assert.Equal("orpesia", store.LoadSnapshot("a")!.Zone);

        cache.Update(cache.Get("a") with { Exp = 42 });
        cache.ImportantEvent("a", 4);
        Assert.Equal(42, store.LoadSnapshot("a")!.Exp);

        cache.Update(cache.Get("a") with { X = 9 });
        var cp = cache.Logout("a", 5);
        Assert.Equal(CheckpointReason.Logout, cp!.Value.Reason);
        Assert.Equal(9, store.LoadSnapshot("a")!.X);
        Assert.Equal(3, store.SnapshotWrites);
    }
}
