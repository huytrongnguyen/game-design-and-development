namespace Course.Navigation.Tests;

/// <summary>Small ASCII maps shared by the tests below, and a procedural generator for the benchmark test.</summary>
internal static class TestMaps
{
    /// <summary>
    /// A 10x5 ground with a wall column at x = 4 that is solid for y = 0..3 and open only at
    /// y = 4 — an agent going from (0, 0) to (9, 0) must detour down to row 4 and back up.
    /// </summary>
    public static readonly string[] DetourMap =
    {
        "....#.....",
        "....#.....",
        "....#.....",
        "....#.....",
        "..........",
    };

    /// <summary>
    /// A 9x11 ground with a single horizontal wall at y = 5, three cells wide open at
    /// x = 3..5. After obstacle inflation the open gap narrows from the sides, so only a
    /// small-radius agent can still fit through its centre column (x = 4).
    /// </summary>
    public static readonly string[] GapMap =
    {
        ".........",
        ".........",
        ".........",
        ".........",
        ".........",
        "###...###",
        ".........",
        ".........",
        ".........",
        ".........",
        ".........",
    };

    /// <summary>
    /// A size x size grid with randomly scattered obstacles (seeded, so deterministic), but with
    /// its top row and right column always left clear — a guaranteed "L-shaped" clear lane from
    /// (0, 0) to (size - 1, size - 1), so the benchmark always has a path to find regardless of
    /// where the random obstacles land.
    /// </summary>
    public static Grid CreateBenchmarkGrid(int size, double obstacleProbability, int seed)
    {
        var random = new Random(seed);
        var walkable = new bool[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                bool guaranteedLane = y == 0 || x == size - 1;
                walkable[x, y] = guaranteedLane || random.NextDouble() >= obstacleProbability;
            }
        }
        return new Grid(walkable);
    }
}
