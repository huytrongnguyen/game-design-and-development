namespace Course.Navigation;

/// <summary>
/// A single "slide along the wall" move step: keeps only the component of an attempted movement
/// delta that runs parallel to a blocking line (a vector projection), discarding the component that
/// was driving the agent through it. This is the standard vector-projection sliding formula;
/// </summary>
public static class SlideMover
{
    public static (double Dx, double Dy) SlideAlong(
        (double X, double Y) wallStart,
        (double X, double Y) wallEnd,
        (double Dx, double Dy) attemptedDelta)
    {
        double wallDirX = wallEnd.X - wallStart.X;
        double wallDirY = wallEnd.Y - wallStart.Y;

        double dot = attemptedDelta.Dx * wallDirX + attemptedDelta.Dy * wallDirY;
        double lengthSquared = wallDirX * wallDirX + wallDirY * wallDirY;
        double ratio = dot / lengthSquared;

        return (wallDirX * ratio, wallDirY * ratio);
    }
}
