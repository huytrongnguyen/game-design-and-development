using System.Threading.Channels;

namespace M17.Net;

/// <summary>
/// One connected player. Sockets allow only one send at a time, so everything bound for the client
/// goes through a bounded outbox that a single writer task drains. A client that cannot keep up is dropped.
/// </summary>
public sealed class ClientSession(string id, int playerId, string name)
{
    private readonly Channel<string> _outbox = Channel.CreateBounded<string>(256);
    private readonly CancellationTokenSource _kill = new();

    public string Id => id;
    public int PlayerId => playerId;
    public string Name => name;
    public ChannelReader<string> Outbox => _outbox.Reader;
    public CancellationToken Killed => _kill.Token;

    public bool TrySend(string json)
    {
        if (_outbox.Writer.TryWrite(json))
        {
            return true;
        }

        _kill.Cancel(); // slow consumer: disconnect rather than buffer without limit
        return false;
    }

    public void Complete() => _outbox.Writer.TryComplete();
}
