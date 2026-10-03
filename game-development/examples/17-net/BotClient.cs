using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace M17.Net;

/// <summary>A headless client: used by the tests and as a tiny load generator.</summary>
public sealed class BotClient : IAsyncDisposable
{
    private readonly ClientWebSocket _socket = new();
    private readonly Channel<Envelope> _received = Channel.CreateUnbounded<Envelope>();
    private Task _receiveLoop = Task.CompletedTask;
    private int _seq;

    private BotClient()
    {
    }

    public int PlayerId { get; private set; }

    public string SessionId { get; private set; } = "";

    /// <summary>Completes when the connection has ended (either side closed it).</summary>
    public Task Closed => _receiveLoop;

    /// <summary>Connects and sends hello. Use <see cref="WaitForAsync"/> to get the welcome.</summary>
    public static async Task<BotClient> ConnectAsync(Uri url, string name, int protocol = Ops.ProtocolVersion)
    {
        var bot = new BotClient();
        await bot._socket.ConnectAsync(url, CancellationToken.None);
        bot._receiveLoop = bot.ReceiveLoopAsync();
        await bot.SendAsync(Ops.Hello, new { name, protocol });
        return bot;
    }

    public async Task<int> SendAsync(string op, object? payload = null)
    {
        var seq = Interlocked.Increment(ref _seq);
        await SendRawAsync(MessageCodec.Serialize(op, seq, payload));
        return seq;
    }

    public Task SendRawAsync(string text) =>
        _socket.SendAsync(Encoding.UTF8.GetBytes(text), WebSocketMessageType.Text, true, CancellationToken.None);

    /// <summary>Reads messages (discarding non-matching ones) until one satisfies the predicate.</summary>
    public async Task<Envelope> WaitForAsync(Func<Envelope, bool> predicate, int timeoutMilliseconds = 3000)
    {
        using var cts = new CancellationTokenSource(timeoutMilliseconds);
        await foreach (var env in _received.Reader.ReadAllAsync(cts.Token))
        {
            if (predicate(env))
            {
                if (env.Op == Ops.Welcome)
                {
                    PlayerId = env.Payload.GetProperty("playerId").GetInt32();
                    SessionId = env.Payload.GetProperty("sessionId").GetString()!;
                }

                return env;
            }
        }

        throw new InvalidOperationException("connection closed before a matching message arrived");
    }

    public Task<Envelope> WaitForOpAsync(string op, int timeoutMilliseconds = 3000) =>
        WaitForAsync(e => e.Op == op, timeoutMilliseconds);

    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[4096];
        try
        {
            while (_socket.State == WebSocketState.Open)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _socket.ReceiveAsync(buffer, CancellationToken.None);
                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                    break;
                }

                if (MessageCodec.TryParse(Encoding.UTF8.GetString(message.ToArray()), out var env))
                {
                    _received.Writer.TryWrite(env!);
                }
            }
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            _received.Writer.TryComplete();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_socket.State == WebSocketState.Open)
        {
            try
            {
                await _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }

        await Task.WhenAny(_receiveLoop, Task.Delay(1000));
        _socket.Dispose();
    }
}
