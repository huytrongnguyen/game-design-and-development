namespace Course.PartyControl;

/// <summary>A 2D point or vector on the ground plane. X points east, Y points north.</summary>
public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double k) => new(a.X * k, a.Y * k);

    public double Length => Math.Sqrt(X * X + Y * Y);

    public double DistanceTo(Vec2 other) => (other - this).Length;

    /// <summary>Rotates counter-clockwise by the given angle in degrees.</summary>
    public Vec2 Rotate(double degrees)
    {
        double r = degrees * Math.PI / 180.0;
        double c = Math.Cos(r), s = Math.Sin(r);
        return new Vec2(X * c - Y * s, X * s + Y * c);
    }
}
