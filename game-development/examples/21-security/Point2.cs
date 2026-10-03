namespace Course.Security;

public readonly record struct Point2(double X, double Y)
{
    public double DistanceTo(Point2 other) => Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
}
