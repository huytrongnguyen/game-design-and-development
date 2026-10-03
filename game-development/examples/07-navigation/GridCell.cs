namespace Course.Navigation;

/// <summary>
/// One cell of a <see cref="Grid"/>. X grows right, Y grows down.
/// </summary>
public readonly record struct GridCell(int X, int Y);
