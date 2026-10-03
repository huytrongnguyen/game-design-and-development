namespace Course.PartyControl;

/// <summary>
/// Slot offsets in the leader's local frame: +X is straight ahead, +Y is to the leader's left.
/// Rotating each offset by the leader's heading turns the same shape toward wherever the leader faces.
/// </summary>
public sealed record Formation(IReadOnlyList<Vec2> FollowerOffsets, Vec2 PetOffset)
{
    /// <summary>Two followers trail behind the leader in a V, the pet sits further back.</summary>
    public static Formation Wedge { get; } = WedgeOf(2);

    /// <summary>A V that fills rows of two behind the leader; the pet takes the slot behind the last row.</summary>
    public static Formation WedgeOf(int followers)
    {
        var offsets = new List<Vec2>();
        for (int i = 0; i < followers; i++)
        {
            double x = -3 - 2 * (i / 2);
            double y = i % 2 == 0 ? 2 : -2;
            offsets.Add(new Vec2(x, y));
        }
        int rows = (followers + 1) / 2;
        return new Formation(offsets, new Vec2(-2 - 2 * rows, 0));
    }

    public Vec2 FollowerSlot(int ordinal, Vec2 leaderPosition, double leaderHeadingDegrees) =>
        leaderPosition + FollowerOffsets[ordinal].Rotate(leaderHeadingDegrees);

    public Vec2 PetSlot(Vec2 leaderPosition, double leaderHeadingDegrees) =>
        leaderPosition + PetOffset.Rotate(leaderHeadingDegrees);
}
