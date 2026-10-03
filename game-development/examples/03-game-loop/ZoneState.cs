namespace Course.GameLoop;

/// <summary>
/// Everything a <see cref="ZoneLoop"/> simulates. Deliberately tiny for the example — a
/// real zone's state would hold entities, positions and stats instead of a balance and a
/// dice total, but the replay property (§5 of the lesson) depends only on "state changes
/// through commands, and nothing reads real time or unseeded randomness," which this
/// minimal shape is enough to demonstrate.
/// </summary>
public sealed class ZoneState
{
    public long Balance { get; set; }
    public long RollTotal { get; set; }

    /// <summary>Every timer id that has fired so far, in firing order.</summary>
    public List<string> FiredTimers { get; } = new();

    /// <summary>Diagnostic-only: the order <see cref="RecordSequenceCommand"/>s were applied in.</summary>
    public List<long> AppliedOrder { get; } = new();

    /// <summary>
    /// A deterministic summary of this state, for replay comparisons ("same seed + same
    /// command log ⇒ identical hash"). Deliberately does <b>not</b> use
    /// <see cref="string.GetHashCode()"/> on <see cref="FiredTimers"/>: .NET randomizes the
    /// string hash seed per process by default, so two separate process runs of the exact
    /// same replay could disagree even though the simulation itself is fully deterministic.
    /// A real replay/checksum system must hash from stable bytes (here: each character's
    /// ordinal value), never from a type whose hash code is allowed to vary.
    /// </summary>
    public long ComputeHash()
    {
        unchecked
        {
            long hash = 17;
            hash = hash * 31 + Balance;
            hash = hash * 31 + RollTotal;

            foreach (var timerId in FiredTimers)
            {
                hash = hash * 31 + StableStringHash(timerId);
            }

            return hash;
        }
    }

    private static long StableStringHash(string value)
    {
        unchecked
        {
            long hash = 5381;
            foreach (char c in value)
            {
                hash = hash * 33 + c;
            }

            return hash;
        }
    }
}
