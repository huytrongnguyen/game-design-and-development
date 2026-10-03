using System.Threading.Channels;

namespace Course.GameLoop;

/// <summary>
/// The one door through which other threads (SignalR hub handlers, a timer thread, a
/// console bot, anything that isn't the zone's own tick loop) may touch a zone: they
/// submit <see cref="ICommand"/>s here, and <see cref="ZoneLoop.Step"/> drains and applies
/// them at the start of the next tick. Everything after that point runs on one thread.
/// </summary>
/// <remarks>
/// <see cref="Channel{T}"/> guarantees thread-safe enqueue/dequeue, but it does
/// <b>not</b> guarantee that concurrent writers land in the channel in any particular
/// order relative to each other — that's a race, by design. To make the *applied* order
/// deterministic and reproducible anyway, every command is stamped with a sequence
/// number assigned by <see cref="Interlocked.Increment(ref long)"/> — an atomic, strictly
/// ordered operation — at the moment it is submitted, and <see cref="DrainOrdered"/> sorts
/// by that number before handing commands to the loop. Two runs that submit the same
/// commands in the same per-thread order always apply them in the same final order, even
/// though the channel itself never promised that.
/// </remarks>
public sealed class CommandQueue
{
    private readonly Channel<CommandEnvelope> _channel = Channel.CreateUnbounded<CommandEnvelope>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    private long _sequence;

    /// <summary>Submits a command. Safe to call from any thread.</summary>
    public void Submit(ICommand command)
    {
        long sequence = Interlocked.Increment(ref _sequence);
        _channel.Writer.TryWrite(new CommandEnvelope(sequence, command));
    }

    /// <summary>
    /// Submits a command built from the sequence number it is about to be assigned —
    /// useful when a command wants to record or depend on its own submission order
    /// (see <see cref="RecordSequenceCommand"/>).
    /// </summary>
    public void Submit(Func<long, ICommand> buildCommand)
    {
        long sequence = Interlocked.Increment(ref _sequence);
        _channel.Writer.TryWrite(new CommandEnvelope(sequence, buildCommand(sequence)));
    }

    /// <summary>
    /// Drains every command currently buffered and returns them ordered by submission
    /// sequence (ascending), oldest first. Called once, at the start of each
    /// <see cref="ZoneLoop.Step"/> — nothing submitted after a drain starts is included
    /// in that tick, by design (it will be picked up on the next tick instead).
    /// </summary>
    public List<ICommand> DrainOrdered()
    {
        var envelopes = new List<CommandEnvelope>();
        while (_channel.Reader.TryRead(out var envelope))
        {
            envelopes.Add(envelope);
        }

        envelopes.Sort(static (a, b) => a.Sequence.CompareTo(b.Sequence));

        var commands = new List<ICommand>(envelopes.Count);
        foreach (var envelope in envelopes)
        {
            commands.Add(envelope.Command);
        }

        return commands;
    }
}
