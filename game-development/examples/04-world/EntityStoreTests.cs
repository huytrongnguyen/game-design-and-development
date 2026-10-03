namespace Course.World;

public class EntityStoreTests
{
    [Fact]
    public void Despawn_ThenQueryOldId_IsRejected()
    {
        var store = new EntityStore();
        var id = store.Spawn();
        store.Set(id, new Health(10, 10));

        Assert.True(store.Despawn(id));

        Assert.False(store.IsAlive(id));
        Assert.False(store.TryGet(id, out Health _));
        Assert.False(store.Set(id, new Health(1, 1)));
        Assert.False(store.Despawn(id));
        Assert.Equal(0, store.AliveCount);
    }

    [Fact]
    public void Spawn_ReusingSlot_StaleIdDoesNotHitNewEntity()
    {
        var store = new EntityStore();
        var old = store.Spawn();
        store.Set(old, new Name("Old"));
        store.Despawn(old);

        var fresh = store.Spawn();
        store.Set(fresh, new Name("Fresh"));

        Assert.Equal(old.Index, fresh.Index);
        Assert.Equal(old.Generation + 1, fresh.Generation);
        Assert.False(store.TryGet(old, out Name _));
        Assert.True(store.TryGet(fresh, out Name n));
        Assert.Equal("Fresh", n.Value);
    }

    [Fact]
    public void Spawn_ReusingSlot_NewEntityHasNoLeftoverComponents()
    {
        var store = new EntityStore();
        var old = store.Spawn();
        store.Set(old, new Health(5, 5));
        store.Despawn(old);

        var fresh = store.Spawn();

        Assert.False(store.Has<Health>(fresh));
    }
}
