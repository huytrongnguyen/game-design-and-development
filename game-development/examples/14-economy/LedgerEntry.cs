namespace Course.Economy;

/// <summary>
/// One row of the permanent record. Rows are never edited or deleted; a mistake is fixed by
/// appending a correcting row. The <see cref="Key"/> names the business event that caused it.
/// </summary>
public sealed record LedgerEntry(long Sequence, string Key, string Account, string Asset, long Delta, string Reason);
