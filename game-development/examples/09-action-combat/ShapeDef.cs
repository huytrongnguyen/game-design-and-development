namespace Course.ActionCombat;

public enum ShapeKind { Circle, Cone, Rectangle, Capsule }

/// <summary>
/// A hit volume, defined as data and placed at the attacker's origin and facing.
/// Circle: <c>Radius</c> around the point <c>Offset</c> ahead of the attacker.
/// Cone: <c>Range</c> and <c>HalfAngleDeg</c> (half of the total opening).
/// Rectangle: <c>Length</c> ahead of the attacker and <c>Width</c> in total, centred on the facing line.
/// Capsule: a segment <c>Length</c> ahead of the attacker, thickened by <c>Radius</c>.
/// </summary>
public sealed record ShapeDef
{
    public ShapeKind Kind { get; init; }
    public double Radius { get; init; }
    public double Offset { get; init; }
    public double Range { get; init; }
    public double HalfAngleDeg { get; init; }
    public double Length { get; init; }
    public double Width { get; init; }
}
