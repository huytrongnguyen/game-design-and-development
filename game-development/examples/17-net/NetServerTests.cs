using System.Text.Json;

namespace M17.Net;

public class NetServerTests
{
    private static async Task<(WebApplication App, Uri Url)> StartAsync(ServerOptions? options = null)
    {
        var app = GameServer.Create("http://127.0.0.1:0", options);
        await app.StartAsync();
        var http = new Uri(app.Urls.First());
        return (app, new Uri($"ws://127.0.0.1:{http.Port}/ws"));
    }

    private static async Task<BotClient> JoinAsync(Uri url, string name)
    {
        var bot = await BotClient.ConnectAsync(url, name);
        await bot.WaitForOpAsync(Ops.Welcome);
        return bot;
    }

    [Fact]
    public async Task Handshake_HelloThenWelcome_ReturnsSessionAndPlayerId()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var bot = await BotClient.ConnectAsync(url, "ann");
            var welcome = await bot.WaitForOpAsync(Ops.Welcome);
            Assert.Equal(1, welcome.Seq);
            Assert.Equal(8, bot.SessionId.Length);
            Assert.True(bot.PlayerId > 0);
            Assert.Equal(50, welcome.Payload.GetProperty("tickMilliseconds").GetInt32());
        }
    }

    [Fact]
    public async Task Handshake_WrongProtocolVersion_IsRejectedAndClosed()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var bot = await BotClient.ConnectAsync(url, "ann", protocol: 99);
            var error = await bot.WaitForOpAsync(Ops.Error);
            Assert.Equal(ErrorCodes.VersionMismatch, error.Payload.GetProperty("code").GetString());
            await bot.Closed.WaitAsync(TimeSpan.FromSeconds(3));
        }
    }

    [Fact]
    public async Task Handshake_FirstMessageNotHello_IsRejected()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            using var socket = new System.Net.WebSockets.ClientWebSocket();
            await socket.ConnectAsync(url, CancellationToken.None);
            await WebSocketText.SendAsync(socket, MessageCodec.Serialize(Ops.Move, 1, new { x = 1, y = 1 }), CancellationToken.None);
            var reply = await WebSocketText.ReadAsync(socket, 4096, CancellationToken.None);
            Assert.Contains(ErrorCodes.HandshakeRequired, reply.Text);
        }
    }

    [Fact]
    public async Task Move_Intent_ProducesPositionUpdateOnNextTick()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var bot = await JoinAsync(url, "ann");
            await bot.WaitForAsync(e => e.Op == Ops.State && e.Payload.GetProperty("full").GetBoolean());

            await bot.SendAsync(Ops.Move, new { x = 60, y = 50 });
            var update = await bot.WaitForAsync(e => e.Op == Ops.State && !e.Payload.GetProperty("full").GetBoolean());

            var me = update.Payload.GetProperty("players").EnumerateArray().Single();
            Assert.Equal(bot.PlayerId, me.GetProperty("id").GetInt32());
            Assert.Equal(52, me.GetProperty("x").GetDouble());
        }
    }

    [Fact]
    public async Task Chat_FromOnePlayer_IsBroadcastToAnother()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var ann = await JoinAsync(url, "ann");
            await using var bob = await JoinAsync(url, "bob");
            await ann.SendAsync(Ops.Chat, new { text = "hello" });
            var chat = await bob.WaitForOpAsync(Ops.Chat);
            Assert.Equal("ann", chat.Payload.GetProperty("from").GetString());
            Assert.Equal("hello", chat.Payload.GetProperty("text").GetString());
        }
    }

    [Fact]
    public async Task UnknownOp_ReturnsErrorEchoingSeq()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var bot = await JoinAsync(url, "ann");
            var seq = await bot.SendAsync("teleport", new { x = 1 });
            var error = await bot.WaitForOpAsync(Ops.Error);
            Assert.Equal(seq, error.Seq);
            Assert.Equal(ErrorCodes.UnknownOp, error.Payload.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task Move_OutOfRange_ReturnsBadPayload()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var bot = await JoinAsync(url, "ann");
            await bot.SendAsync(Ops.Move, new { x = 5000, y = 1 });
            var error = await bot.WaitForOpAsync(Ops.Error);
            Assert.Equal(ErrorCodes.BadPayload, error.Payload.GetProperty("code").GetString());
        }
    }

    [Fact]
    public async Task Flood_SixtyMessagesAtOnce_IsRateLimitedAndDisconnected()
    {
        var (app, url) = await StartAsync();
        await using (app)
        {
            await using var bot = await JoinAsync(url, "spammer");
            for (var i = 0; i < 60; i++)
            {
                await bot.SendAsync(Ops.Ping);
            }

            var error = await bot.WaitForAsync(e => e.Op == Ops.Error);
            Assert.Equal(ErrorCodes.RateLimited, error.Payload.GetProperty("code").GetString());
            await bot.Closed.WaitAsync(TimeSpan.FromSeconds(5));
        }
    }
}
