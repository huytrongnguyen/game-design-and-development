using System.Security.Cryptography;
using System.Text;

namespace Course.Security;

/// <summary>Server-side check of a submitted code. It accepts a small clock drift, refuses to
/// accept the same time step twice (a shoulder-surfed or phished code is single-use) and locks
/// the account after repeated failures.</summary>
public sealed class TotpVerifier
{
    private readonly int _driftSteps;
    private readonly int _maxFailures;
    private readonly Dictionary<string, long> _lastUsedCounter = new();
    private readonly Dictionary<string, int> _failures = new();

    public TotpVerifier(int driftSteps = 1, int maxFailures = 5)
    {
        _driftSteps = driftSteps;
        _maxFailures = maxFailures;
    }

    public TotpResult Verify(string accountId, ReadOnlySpan<byte> secret, string submitted, long unixSeconds)
    {
        if (_failures.GetValueOrDefault(accountId) >= _maxFailures) return TotpResult.LockedOut;

        var now = Totp.CounterFor(unixSeconds);
        var last = _lastUsedCounter.GetValueOrDefault(accountId, -1);
        var sawOldMatch = false;

        for (var c = now - _driftSteps; c <= now + _driftSteps; c++)
        {
            var expected = Totp.ForCounter(secret, c, submitted.Length);
            // Constant-time compare so timing does not leak how many digits were right.
            if (!CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(submitted)))
                continue;
            if (c <= last) { sawOldMatch = true; continue; }

            _lastUsedCounter[accountId] = c;
            _failures.Remove(accountId);
            return TotpResult.Valid;
        }

        if (sawOldMatch) return TotpResult.AlreadyUsed;
        _failures[accountId] = _failures.GetValueOrDefault(accountId) + 1;
        return TotpResult.Invalid;
    }
}
