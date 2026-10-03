namespace Course.Economy;

public enum ApplyStatus
{
    Applied,
    /// <summary>The idempotency key was seen before: nothing changed, and that is the correct outcome of a retry.</summary>
    Duplicate,
    InsufficientFunds,
    Invalid,
}
