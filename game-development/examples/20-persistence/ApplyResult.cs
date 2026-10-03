namespace Course.Persistence;

/// <summary>Result of one transactional operation. A duplicate is a success: the operation
/// already took effect earlier, which is exactly what a retrying caller wants to know.</summary>
public readonly record struct ApplyResult(ApplyOutcome Outcome, string Reason = "")
{
    public bool Ok => Outcome != ApplyOutcome.Rejected;
}
