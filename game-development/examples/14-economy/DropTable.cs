namespace Course.Economy;

/// <summary>
/// A list of independent rolls. One kill can drop nothing, one item or several, because each
/// entry is rolled on its own. Guaranteed entries (denominator 1) do not consume a random number.
/// </summary>
public sealed class DropTable(IReadOnlyList<DropEntry> entries)
{
    public IReadOnlyList<DropResult> Roll(SplitMixRng rng, DropContext? contextOrNull = null)
    {
        var context = contextOrNull ?? DropContext.Neutral;
        var drops = new List<DropResult>();
        foreach (var entry in entries)
        {
            var denominator = context.Effective(entry.Denominator);
            if (denominator == 1 || rng.NextInt(denominator) == 0)
                drops.Add(new DropResult(entry.ItemId, entry.Quantity));
        }
        return drops;
    }
}
