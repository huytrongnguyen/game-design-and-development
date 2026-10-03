namespace M17.Net;

public class MessageCodecTests
{
    [Fact]
    public void TryParse_ValidMove_ReadsOpSeqAndPayload()
    {
        Assert.True(MessageCodec.TryParse("""{"op":"move","seq":7,"payload":{"x":10,"y":20.5}}""", out var env));
        Assert.Equal("move", env!.Op);
        Assert.Equal(7, env.Seq);
        Assert.True(MessageCodec.TryReadPayload<MovePayload>(env, out var move));
        Assert.Equal(10, move!.X);
        Assert.Equal(20.5, move.Y);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[1,2]")]
    [InlineData("""{"seq":1}""")]
    [InlineData("""{"op":5}""")]
    public void TryParse_Malformed_ReturnsFalse(string text) =>
        Assert.False(MessageCodec.TryParse(text, out _));

    [Fact]
    public void TryReadPayload_MissingField_LeavesItNull()
    {
        MessageCodec.TryParse("""{"op":"move","payload":{"x":1}}""", out var env);
        Assert.True(MessageCodec.TryReadPayload<MovePayload>(env!, out var move));
        Assert.Null(move!.Y);
    }

    [Fact]
    public void Serialize_Error_UsesCamelCaseEnvelope() =>
        Assert.Equal(
            """{"op":"error","seq":3,"payload":{"code":"unknown_op","message":"x"}}""",
            MessageCodec.Error(3, ErrorCodes.UnknownOp, "x"));
}
