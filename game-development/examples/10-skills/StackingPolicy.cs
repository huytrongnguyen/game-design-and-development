namespace Course.Skills;

/// <summary>What happens when a buff that is already active is applied again. Stored as data on the buff, not inferred.</summary>
public enum StackingPolicy
{
    /// <summary>One instance; re-applying resets its duration.</summary>
    Refresh,

    /// <summary>One instance with a stack counter up to MaxStacks; each application adds a stack and resets the duration.</summary>
    StackToMax,

    /// <summary>Every application is its own instance with its own timer (up to MaxStacks instances; the oldest is replaced when full).</summary>
    Independent,
}
