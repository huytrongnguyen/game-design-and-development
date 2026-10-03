namespace Course.Sync;

public class AoiGridTests
{
    [Fact]
    public void Query_ReturnsOnlyEntitiesInsideTheCircle()
    {
        var grid = new AoiGrid(50);
        grid.Insert(new EntityId(1), 0, 0);
        grid.Insert(new EntityId(2), 99, 0);     // inside radius 100
        grid.Insert(new EntityId(3), 80, 80);    // distance ~113, same cells but outside
        grid.Insert(new EntityId(4), 500, 500);

        var found = new List<EntityId>();
        grid.Query(0, 0, 100, found);

        Assert.Equal(new[] { 1, 2 }, found.Select(i => i.Value).OrderBy(v => v));
    }

    [Fact]
    public void Move_AcrossCellBorder_IsFoundInTheNewCellOnly()
    {
        var grid = new AoiGrid(50);
        grid.Insert(new EntityId(1), 10, 10);

        grid.Move(new EntityId(1), 410, 10);

        var near = new List<EntityId>();
        grid.Query(10, 10, 20, near);
        var far = new List<EntityId>();
        grid.Query(410, 10, 20, far);
        Assert.Empty(near);
        Assert.Single(far);
    }

    [Fact]
    public void Query_NegativeCoordinates_Work()
    {
        var grid = new AoiGrid(50);
        grid.Insert(new EntityId(1), -10, -10);

        var found = new List<EntityId>();
        grid.Query(0, 0, 20, found);

        Assert.Single(found);
    }

    [Fact]
    public void Remove_EntityIsNoLongerFound()
    {
        var grid = new AoiGrid(50);
        grid.Insert(new EntityId(1), 0, 0);

        grid.Remove(new EntityId(1));

        var found = new List<EntityId>();
        grid.Query(0, 0, 100, found);
        Assert.Empty(found);
        Assert.Equal(0, grid.Count);
    }
}
