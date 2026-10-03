using System.Net.WebSockets;

namespace M17.Net;

/// <summary>
/// The network edge. It handshakes, validates and rate-limits, then turns requests into intents.
/// It never changes game state itself.
/// </summary>
public sealed class GameSocketHandler(ZoneService zone, SessionRegistry registry, ServerOptions options)
{
    private static int _nextPlayerId;

    public async Task RunAsync(WebSocket socket, CancellationToken aborted)
    {
        var session = await HandshakeAsync(socket, aborted);
        if (session is null)
        {
            return;
        }

        registry.Add(session);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(aborted, session.Killed);
        var writer = WriteLoopAsync(socket, session, linked.Token);
        zone.Zone.Enqueue(Intent.Join(session.PlayerId, session.Name));
        try
        {
            await ReadLoopAsync(socket, session, linked.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
        finally
        {
            zone.Zone.Enqueue(Intent.Leave(session.PlayerId));
            registry.Remove(session);
            session.Complete();
            await writer;
        }
    }

    private async Task<ClientSession?> HandshakeAsync(WebSocket socket, CancellationToken aborted)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(aborted);
        timeout.CancelAfter(options.HelloTimeoutMilliseconds);
        try
        {
            var read = await WebSocketText.ReadAsync(socket, options.MaxMessageBytes, timeout.Token);
            if (read.Status != ReadStatus.Text)
            {
                await CloseAsync(socket, WebSocketCloseStatus.ProtocolError, "expected hello");
                return null;
            }

            if (!MessageCodec.TryParse(read.Text, out var env) || env!.Op != Ops.Hello)
            {
                await RejectAsync(socket, 0, ErrorCodes.HandshakeRequired, "first message must be hello");
                return null;
            }

            if (!MessageCodec.TryReadPayload<HelloPayload>(env, out var hello) || !ValidName(hello!.Name))
            {
                await RejectAsync(socket, env.Seq, ErrorCodes.BadPayload, "name must be 1-16 letters, digits or _");
                return null;
            }

            if (hello.Protocol != Ops.ProtocolVersion)
            {
                await RejectAsync(socket, env.Seq, ErrorCodes.VersionMismatch, $"server speaks protocol {Ops.ProtocolVersion}");
                return null;
            }

            var playerId = Interlocked.Increment(ref _nextPlayerId);
            var session = new ClientSession(Guid.NewGuid().ToString("N")[..8], playerId, hello.Name!);
            session.TrySend(MessageCodec.Serialize(Ops.Welcome, env.Seq, new
            {
                sessionId = session.Id,
                playerId,
                protocol = Ops.ProtocolVersion,
                tickMilliseconds = options.TickMilliseconds,
                tick = zone.Zone.CurrentTick,
            }));
            return session;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (WebSocketException)
        {
            return null;
        }
    }

    private async Task ReadLoopAsync(WebSocket socket, ClientSession session, CancellationToken ct)
    {
        var bucket = new TokenBucket(options.RateBurst, options.RatePerSecond, () => Environment.TickCount64);
        var strikes = 0;
        while (!ct.IsCancellationRequested)
        {
            var read = await WebSocketText.ReadAsync(socket, options.MaxMessageBytes, ct);
            if (read.Status == ReadStatus.Closed)
            {
                return;
            }

            if (read.Status != ReadStatus.Text)
            {
                await CloseAsync(socket, read.Status == ReadStatus.TooBig
                    ? WebSocketCloseStatus.MessageTooBig
                    : WebSocketCloseStatus.InvalidPayloadData, "unacceptable message");
                return;
            }

            if (!bucket.TryTake())
            {
                if (++strikes >= options.MaxStrikes)
                {
                    await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, "rate limit");
                    return;
                }

                if (strikes == 1)
                {
                    session.TrySend(MessageCodec.Error(0, ErrorCodes.RateLimited, "slow down"));
                }

                continue;
            }

            Dispatch(session, read.Text);
        }
    }

    private void Dispatch(ClientSession session, string text)
    {
        if (!MessageCodec.TryParse(text, out var env))
        {
            session.TrySend(MessageCodec.Error(0, ErrorCodes.BadJson, "not a valid envelope"));
            return;
        }

        switch (env!.Op)
        {
            case Ops.Ping:
                session.TrySend(MessageCodec.Serialize(Ops.Pong, env.Seq, new { tick = zone.Zone.CurrentTick }));
                break;
            case Ops.Move:
                if (MessageCodec.TryReadPayload<MovePayload>(env, out var move) && ValidMove(move!))
                {
                    zone.Zone.Enqueue(Intent.Move(session.PlayerId, move!.X!.Value, move.Y!.Value));
                }
                else
                {
                    session.TrySend(MessageCodec.Error(env.Seq, ErrorCodes.BadPayload, "move needs x and y in 0..100"));
                }

                break;
            case Ops.Chat:
                if (MessageCodec.TryReadPayload<ChatPayload>(env, out var chat) && !string.IsNullOrWhiteSpace(chat!.Text))
                {
                    zone.Zone.Enqueue(Intent.Chat(session.PlayerId, chat.Text.Trim()));
                }
                else
                {
                    session.TrySend(MessageCodec.Error(env.Seq, ErrorCodes.BadPayload, "chat needs text"));
                }

                break;
            case Ops.Hello:
                session.TrySend(MessageCodec.Error(env.Seq, ErrorCodes.AlreadyHandshaken, "hello was already accepted"));
                break;
            default:
                session.TrySend(MessageCodec.Error(env.Seq, ErrorCodes.UnknownOp, $"unknown op '{env.Op}'"));
                break;
        }
    }

    private static bool ValidMove(MovePayload m) =>
        m.X is { } x && m.Y is { } y
        && double.IsFinite(x) && double.IsFinite(y)
        && x is >= 0 and <= ZoneLoop.WorldSize && y is >= 0 and <= ZoneLoop.WorldSize;

    private static bool ValidName(string? name) =>
        name is { Length: >= 1 and <= 16 } && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_');

    private static async Task WriteLoopAsync(WebSocket socket, ClientSession session, CancellationToken ct)
    {
        try
        {
            await foreach (var json in session.Outbox.ReadAllAsync(ct))
            {
                await WebSocketText.SendAsync(socket, json, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
    }

    private static async Task RejectAsync(WebSocket socket, int seq, string code, string message)
    {
        await WebSocketText.SendAsync(socket, MessageCodec.Error(seq, code, message), CancellationToken.None);
        await CloseAsync(socket, WebSocketCloseStatus.PolicyViolation, code);
    }

    private static async Task CloseAsync(WebSocket socket, WebSocketCloseStatus status, string reason)
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseOutputAsync(status, reason, CancellationToken.None);
            }
            catch (WebSocketException)
            {
            }
        }
    }
}
