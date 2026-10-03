namespace Course.Navigation;

/// <summary>
/// Sample agent size classes: four circular footprints, in world units. Real games choose their own
/// classes (often as data); a small fixed set keeps the number of inflated grids low.
/// </summary>
public static class AgentSizes
{
    public const int Small = 10;
    public const int Medium = 20;
    public const int Large = 40;
    public const int XLarge = 80;

    /// <summary>Converts a world-unit radius into whole grid cells, rounding up (ceiling division).</summary>
    public static int ToCells(int radiusUnits, int cellSizeUnits) =>
        (radiusUnits + cellSizeUnits - 1) / cellSizeUnits;
}
