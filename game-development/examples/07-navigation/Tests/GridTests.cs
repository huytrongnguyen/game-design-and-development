namespace Course.Navigation.Tests;

public class GridTests
{
    [Fact]
    public void FromAscii_ParsesWallsAndOpenCells()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);

        Assert.Equal(10, grid.Width);
        Assert.Equal(5, grid.Height);
        Assert.False(grid.IsWalkable(new GridCell(4, 0))); // '#'
        Assert.True(grid.IsWalkable(new GridCell(4, 4)));  // the gap row
    }

    [Fact]
    public void InflateObstacles_RadiusOne_BlocksCellsAdjacentToWall()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);
        var inflated = grid.InflateObstacles(radiusCells: 1);

        // (3, 0) and (5, 0) are orthogonally adjacent to the wall at (4, 0): blocked.
        Assert.False(inflated.IsWalkable(new GridCell(3, 0)));
        Assert.False(inflated.IsWalkable(new GridCell(5, 0)));
        // (0, 0) is three cells away: still open.
        Assert.True(inflated.IsWalkable(new GridCell(0, 0)));
    }

    [Fact]
    public void InflateObstacles_RadiusZero_ReturnsSameWalkability()
    {
        var grid = Grid.FromAscii(TestMaps.DetourMap);
        var inflated = grid.InflateObstacles(radiusCells: 0);

        for (int x = 0; x < grid.Width; x++)
            for (int y = 0; y < grid.Height; y++)
                Assert.Equal(grid.IsWalkable(new GridCell(x, y)), inflated.IsWalkable(new GridCell(x, y)));
    }

    [Fact]
    public void Neighbors_DiagonalThatWouldClipAWallCorner_IsNotOffered()
    {
        // The diagonal target (1, 1) is open, but one of the two orthogonal cells beside the
        // diagonal step, (0, 1), is a wall — stepping there would clip the wall's corner, so
        // the diagonal move must not be offered even though its destination is walkable.
        var grid = Grid.FromAscii(new[]
        {
            "...",
            "#..",
            "...",
        });

        var fromTopLeft = grid.Neighbors(new GridCell(0, 0)).Select(n => n.Cell).ToHashSet();

        Assert.DoesNotContain(new GridCell(1, 1), fromTopLeft);
    }

    [Fact]
    public void Neighbors_DiagonalWithBothSidesOpen_IsOffered()
    {
        var grid = Grid.FromAscii(new[]
        {
            "...",
            "...",
            "...",
        });

        var fromCentre = grid.Neighbors(new GridCell(1, 1)).ToList();

        Assert.Contains(fromCentre, n => n.Cell == new GridCell(2, 2) && n.Cost > 1.0);
    }
}
