namespace Course.Economy;

/// <summary>
/// An append-only record of every change to currencies and items, with idempotency keys.
/// A request is a group of lines applied together or not at all. Applying the same key twice is a
/// no-op, so a retry after a timeout cannot pay a reward twice. Accounts whose name starts with
/// "system:" (a faucet that creates money, a sink that destroys it) may go negative; players may not.
/// </summary>
public sealed class Ledger
{
    private readonly List<LedgerEntry> _entries = [];
    private readonly HashSet<string> _keys = [];
    private readonly Dictionary<(string Account, string Asset), long> _balances = new();

    public IReadOnlyList<LedgerEntry> Entries => _entries;

    public ApplyStatus TryApply(string key, string reason, IReadOnlyList<LedgerLine> lines)
    {
        if (string.IsNullOrEmpty(key) || lines.Count == 0 || lines.Any(l => l.Delta == 0))
            return ApplyStatus.Invalid;
        if (_keys.Contains(key))
            return ApplyStatus.Duplicate;

        // Validate the whole group against projected balances before writing anything.
        var projected = new Dictionary<(string, string), long>();
        foreach (var line in lines)
        {
            var slot = (line.Account, line.Asset);
            projected[slot] = projected.GetValueOrDefault(slot, Balance(line.Account, line.Asset)) + line.Delta;
        }
        foreach (var ((account, _), balance) in projected)
            if (balance < 0 && !account.StartsWith("system:", StringComparison.Ordinal))
                return ApplyStatus.InsufficientFunds;

        foreach (var line in lines)
        {
            _entries.Add(new LedgerEntry(_entries.Count + 1, key, line.Account, line.Asset, line.Delta, reason));
            var slot = (line.Account, line.Asset);
            _balances[slot] = _balances.GetValueOrDefault(slot) + line.Delta;
        }
        _keys.Add(key);
        return ApplyStatus.Applied;
    }

    /// <summary>The fast, cached balance.</summary>
    public long Balance(string account, string asset) => _balances.GetValueOrDefault((account, asset));

    /// <summary>The balance recomputed from the entries: the source of truth the cache must always equal.</summary>
    public long BalanceFromEntries(string account, string asset) =>
        _entries.Where(e => e.Account == account && e.Asset == asset).Sum(e => e.Delta);
}
