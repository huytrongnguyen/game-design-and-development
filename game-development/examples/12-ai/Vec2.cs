namespace M12.Ai;

/// <summary>A 2D point or offset in world units.</summary>
public readonly record struct Vec2(double X, double Y)
{
    public double DistanceTo(Vec2 other)
    {
        var dx = other.X - X;
        var dy = other.Y - Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Moves at most <paramref name="maxStep"/> toward <paramref name="target"/> without overshooting.</summary>
    public Vec2 MoveToward(Vec2 target, double maxStep)
    {
        var d = DistanceTo(target);
        if (d <= maxStep || d == 0) return target;
        var k = maxStep / d;
        return new Vec2(X + (target.X - X) * k, Y + (target.Y - Y) * k);
    }
}
