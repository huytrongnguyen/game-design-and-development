namespace Course.ActionCombat;

/// <summary>A 2D vector on the ground plane. Angles are in degrees; 0 degrees points along +X.</summary>
public readonly record struct Vec2(double X, double Y)
{
    public static Vec2 operator +(Vec2 a, Vec2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vec2 operator -(Vec2 a, Vec2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vec2 operator *(Vec2 a, double s) => new(a.X * s, a.Y * s);

    public double Length => Math.Sqrt(X * X + Y * Y);
    public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
    public static double Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>Unit vector for an angle in degrees.</summary>
    public static Vec2 FromAngle(double degrees)
    {
        double r = degrees * Math.PI / 180.0;
        return new(Math.Cos(r), Math.Sin(r));
    }
}
