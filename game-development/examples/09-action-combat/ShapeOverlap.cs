namespace Course.ActionCombat;

/// <summary>
/// Overlap tests between a hit shape and a target's hurtbox, which is a circle (centre + radius).
/// Circles are the common choice for hurtboxes: cheap, and rotation does not matter.
/// </summary>
public static class ShapeOverlap
{
    public static bool Hits(ShapeDef shape, Vec2 origin, double facingDeg, Vec2 center, double targetRadius)
    {
        Vec2 dir = Vec2.FromAngle(facingDeg);
        Vec2 d = center - origin;
        switch (shape.Kind)
        {
            case ShapeKind.Circle:
            {
                Vec2 c = origin + dir * shape.Offset;
                return (center - c).Length <= shape.Radius + targetRadius;
            }
            case ShapeKind.Capsule:
            {
                double t = Math.Clamp(Vec2.Dot(d, dir), 0, shape.Length);
                Vec2 closest = origin + dir * t;
                return (center - closest).Length <= shape.Radius + targetRadius;
            }
            case ShapeKind.Rectangle:
            {
                // Move the target into the rectangle's local space: x along the facing, y sideways.
                double x = Vec2.Dot(d, dir);
                double y = Vec2.Cross(dir, d);
                double cx = Math.Clamp(x, 0, shape.Length);
                double cy = Math.Clamp(y, -shape.Width / 2, shape.Width / 2);
                return Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) <= targetRadius;
            }
            case ShapeKind.Cone:
            {
                double dist = d.Length;
                if (dist <= targetRadius) return true;               // the target covers the apex
                if (dist - targetRadius > shape.Range) return false;  // too far
                double angle = Math.Abs(Math.Atan2(Vec2.Cross(dir, d), Vec2.Dot(dir, d))) * 180.0 / Math.PI;
                // A target with size is still hit when only its edge is inside the cone.
                double slack = Math.Asin(Math.Min(1.0, targetRadius / dist)) * 180.0 / Math.PI;
                return angle - slack <= shape.HalfAngleDeg;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }
    }
}
