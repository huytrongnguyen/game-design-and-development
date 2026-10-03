using System.Text;

namespace Course.Security;

public class SessionChannelTests
{
    private static readonly byte[] Key = Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef");

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Open_FreshMessagesInOrder_Ok()
    {
        var client = new SessionChannel(Key);
        var server = new SessionChannel(Key);

        var m1 = client.Seal(Bytes("move 1"));
        var m2 = client.Seal(Bytes("move 2"));

        Assert.Equal(OpenResult.Ok, server.Open(m1, out var p1));
        Assert.Equal(OpenResult.Ok, server.Open(m2, out _));
        Assert.Equal("move 1", Encoding.UTF8.GetString(p1));
        Assert.Equal((1L, 2L), (m1.Seq, m2.Seq));
    }

    [Fact]
    public void Open_SameMessageTwice_SecondIsReplayed()
    {
        var client = new SessionChannel(Key);
        var server = new SessionChannel(Key);
        var buy = client.Seal(Bytes("buy potion"));

        Assert.Equal(OpenResult.Ok, server.Open(buy, out _));
        Assert.Equal(OpenResult.Replayed, server.Open(buy, out _));   // an attacker re-sends the recorded packet
    }

    [Fact]
    public void Open_OldMessageAfterNewer_Replayed()
    {
        var client = new SessionChannel(Key);
        var server = new SessionChannel(Key);
        var m1 = client.Seal(Bytes("a"));
        var m2 = client.Seal(Bytes("b"));

        server.Open(m2, out _);
        Assert.Equal(OpenResult.Replayed, server.Open(m1, out _));
    }

    [Fact]
    public void Open_EditedPayload_Tampered()
    {
        var client = new SessionChannel(Key);
        var server = new SessionChannel(Key);
        var m = client.Seal(Bytes("sell 1 potion"));

        var forged = m with { Payload = Bytes("sell 9 potion") };

        Assert.Equal(OpenResult.Tampered, server.Open(forged, out _));
    }

    [Fact]
    public void Open_EditedSequenceNumber_Tampered()
    {
        var client = new SessionChannel(Key);
        var server = new SessionChannel(Key);
        var m = client.Seal(Bytes("x"));

        Assert.Equal(OpenResult.Tampered, server.Open(m with { Seq = 99 }, out _));
    }

    [Fact]
    public void Open_MessageFromAnotherSessionKey_Tampered()
    {
        var other = new SessionChannel(Encoding.ASCII.GetBytes("ffffffffffffffffffffffffffffffff"));
        var server = new SessionChannel(Key);

        Assert.Equal(OpenResult.Tampered, server.Open(other.Seal(Bytes("hi")), out _));
    }

    [Fact]
    public void Open_TamperedMessage_DoesNotAdvanceTheSequence()
    {
        var client = new SessionChannel(Key);
        var server = new SessionChannel(Key);
        var m = client.Seal(Bytes("x"));

        server.Open(m with { Seq = 99 }, out _);                 // forged, rejected
        Assert.Equal(OpenResult.Ok, server.Open(m, out _));      // the genuine message 1 still works
    }
}
