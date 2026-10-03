namespace Course.Skills;

/// <summary>How a modifier combines with the stat it targets.</summary>
public enum ModifierOp
{
    /// <summary>Flat amount added to the base value before percentages are applied.</summary>
    Add,

    /// <summary>Percentage points; all PercentAdd modifiers on one stat are summed, then applied once.</summary>
    PercentAdd,
}
