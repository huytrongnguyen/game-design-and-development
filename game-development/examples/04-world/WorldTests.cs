namespace Course.World;

public class WorldTests
{
    private static readonly ZoneId Town = new(1);
    private static readonly ZoneId Field = new(2);

    private static World Make()
    {
        var world = new World();
        world.AddZone(Town, 100, 100, 10);
        world.AddZone(Field, 200, 200, 20);
        return world;
    }

    [Fact]
    public void Spawn_InUnknownZone_ReturnsNull()
    {
        Assert.Null(Make().Spawn(new ZoneId(99), "X", new Position(1, 1), 10));
    }

    [Fact]
    public void Spawn_SetsStandardComponentsAndEmitsEntered()
    {
        var world = Make();

        var id = world.Spawn(Town, "Mira", new Position(5, 5), 120)!.Value;

        Assert.True(world.Store.TryGet(id, out Health h));
        Assert.Equal(new Health(120, 120), h);
        Assert.Equal([new WorldEvent(WorldEventKind.Entered, id, Town)], world.DrainEvents());
    }

    [Fact]
    public void Transfer_BetweenZones_EmitsLeaveThenEnter()
    {
        var world = Make();
        var id = world.Spawn(Town, "Mira", new Position(5, 5), 120)!.Value;
        world.DrainEvents();

        Assert.True(world.Transfer(id, Field, new Position(150, 150)));

        Assert.Equal(
            [new WorldEvent(WorldEventKind.Left, id, Town), new WorldEvent(WorldEventKind.Entered, id, Field)],
            world.DrainEvents());
        Assert.Equal(Field, world.ZoneOf(id));
        Assert.Equal(0, world.GetZone(Town)!.Count);
        Assert.Equal(1, world.GetZone(Field)!.Count);
        Assert.Equal((7, 7), world.GetZone(Field)!.CellOf(id));
    }

    [Fact]
    public void Transfer_ToOutOfBoundsPosition_ChangesNothing()
    {
        var world = Make();
        var id = world.Spawn(Town, "Mira", new Position(5, 5), 120)!.Value;
        world.DrainEvents();

        Assert.False(world.Transfer(id, Field, new Position(500, 5)));

        Assert.Equal(Town, world.ZoneOf(id));
        Assert.Empty(world.DrainEvents());
    }

    [Fact]
    public void Despawn_EmitsLeftAndRejectsSecondDespawn()
    {
        var world = Make();
        var id = world.Spawn(Town, "Mira", new Position(5, 5), 120)!.Value;
        world.DrainEvents();

        Assert.True(world.Despawn(id));
        Assert.False(world.Despawn(id));

        Assert.Equal([new WorldEvent(WorldEventKind.Left, id, Town)], world.DrainEvents());
    }

    [Fact]
    public void Move_WithStaleId_IsRejected()
    {
        var world = Make();
        var old = world.Spawn(Town, "Old", new Position(5, 5), 10)!.Value;
        world.Despawn(old);
        var fresh = world.Spawn(Town, "Fresh", new Position(5, 5), 10)!.Value;

        Assert.False(world.Move(old, new Position(50, 50)));

        Assert.True(world.Store.TryGet(fresh, out Position p));
        Assert.Equal(new Position(5, 5), p);
    }
}
