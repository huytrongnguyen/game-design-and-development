namespace Course.World;

public class ZoneTests
{
    // 100x100 zone, 10-unit cells.
    private static (World World, Zone Zone) Make()
    {
        var world = new World();
        var zone = world.AddZone(new ZoneId(1), 100, 100, 10);
        return (world, zone);
    }

    private static List<EntityId> Query(Zone zone, float x, float y, float r)
    {
        var list = new List<EntityId>();
        zone.Query(new Position(x, y), r, list);
        return list;
    }

    [Fact]
    public void Query_Radius10At5_5_ReturnsExactlyTheEntitiesInRange()
    {
        var (w, z) = Make();
        var a = w.Spawn(z.Id, "A", new Position(5, 5), 10)!.Value;    // distance 0
        var b = w.Spawn(z.Id, "B", new Position(14, 5), 10)!.Value;   // distance 9
        var c = w.Spawn(z.Id, "C", new Position(15, 5), 10)!.Value;   // distance exactly 10
        w.Spawn(z.Id, "D", new Position(25, 5), 10);                  // distance 20
        w.Spawn(z.Id, "E", new Position(5, 30), 10);                  // distance 25
        w.Spawn(z.Id, "F", new Position(12, 12), 10);                 // distance ~9.9 -> in
        var f = new EntityId(5, 0);

        var hits = Query(z, 5, 5, 10);

        Assert.Equal([a, b, c, f], hits);
    }

    [Fact]
    public void Query_DiagonalCornerOutsideCircle_IsExcludedEvenInsideBoundingBox()
    {
        var (w, z) = Make();
        w.Spawn(z.Id, "Corner", new Position(14, 14), 10);   // inside the 20x20 box around (5,5)... distance ~12.7

        Assert.Empty(Query(z, 5, 5, 10));
    }

    [Fact]
    public void Query_NearZoneEdge_ClampsWithoutError()
    {
        var (w, z) = Make();
        var a = w.Spawn(z.Id, "A", new Position(1, 1), 10)!.Value;

        Assert.Equal([a], Query(z, 0, 0, 50));
    }

    [Fact]
    public void Move_AcrossCellBoundary_UpdatesGrid()
    {
        var (w, z) = Make();
        var a = w.Spawn(z.Id, "A", new Position(9, 5), 10)!.Value;
        Assert.Equal((0, 0), z.CellOf(a));
        Assert.Equal(1, z.CountInCell(0, 0));

        Assert.True(w.Move(a, new Position(11, 5)));

        Assert.Equal((1, 0), z.CellOf(a));
        Assert.Equal(0, z.CountInCell(0, 0));
        Assert.Equal(1, z.CountInCell(1, 0));
    }

    [Fact]
    public void Move_WithinSameCell_LeavesGridUntouched()
    {
        var (w, z) = Make();
        var a = w.Spawn(z.Id, "A", new Position(2, 2), 10)!.Value;

        w.Move(a, new Position(8, 8));

        Assert.False(z.Update(a));
        Assert.Equal((0, 0), z.CellOf(a));
        Assert.Equal(1, z.CountInCell(0, 0));
    }

    [Fact]
    public void Move_OutOfBounds_IsRejectedAndPositionKept()
    {
        var (w, z) = Make();
        var a = w.Spawn(z.Id, "A", new Position(5, 5), 10)!.Value;

        Assert.False(w.Move(a, new Position(100, 5)));

        Assert.True(w.Store.TryGet(a, out Position p));
        Assert.Equal(new Position(5, 5), p);
    }

    [Fact]
    public void Despawn_RemovesEntityFromQueries()
    {
        var (w, z) = Make();
        var a = w.Spawn(z.Id, "A", new Position(5, 5), 10)!.Value;

        w.Despawn(a);

        Assert.Empty(Query(z, 5, 5, 10));
        Assert.Equal(0, z.Count);
    }
}
