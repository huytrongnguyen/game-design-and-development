namespace Course.Topology.Tests;

public class TokenTests
{
    [Fact]
    public void Login_WrongPassword_IssuesNoToken()
    {
        var w = new TestWorld();
        var r = w.Login.Login("mira", "wrong");
        Assert.False(r.Ok);
        Assert.Null(r.Token);
    }

    [Fact]
    public void Validate_FreshToken_ReturnsAccountAndExpiry()
    {
        var w = new TestWorld();
        var v = w.Tokens.Validate(w.LoginMira());
        Assert.Equal(TokenStatus.Valid, v.Status);
        Assert.Equal("mira", v.Claims!.AccountId);
        Assert.Equal(1_060, v.Claims.ExpiresAtSeconds); // clock 1000 + ttl 60
    }

    [Fact]
    public void Validate_AfterTtl_ReturnsExpired()
    {
        var w = new TestWorld();
        var token = w.LoginMira();
        w.Clock.Advance(59);
        Assert.Equal(TokenStatus.Valid, w.Tokens.Validate(token).Status);
        w.Clock.Advance(1); // exactly at expiry
        Assert.Equal(TokenStatus.Expired, w.Tokens.Validate(token).Status);
    }

    [Fact]
    public void Validate_PayloadSwappedToAnotherAccount_ReturnsBadSignature()
    {
        var w = new TestWorld();
        var mine = w.LoginMira();
        var theirs = w.Login.Login("ned", "pw2").Token!;
        var forged = theirs.Split('.')[0] + "." + mine.Split('.')[1];
        Assert.Equal(TokenStatus.BadSignature, w.Tokens.Validate(forged).Status);
    }

    [Fact]
    public void Validate_SignatureFlipped_ReturnsBadSignature()
    {
        var w = new TestWorld();
        var parts = w.LoginMira().Split('.');
        var sig = parts[1];
        var tampered = parts[0] + "." + (sig[0] == 'A' ? 'B' : 'A') + sig[1..];
        Assert.Equal(TokenStatus.BadSignature, w.Tokens.Validate(tampered).Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-dot")]
    [InlineData("a.b.c")]
    [InlineData("!!!.???")]
    public void Validate_Garbage_ReturnsMalformed(string token)
    {
        var w = new TestWorld();
        Assert.Equal(TokenStatus.Malformed, w.Tokens.Validate(token).Status);
    }

    [Fact]
    public void Validate_TokenFromAnotherKey_ReturnsBadSignature()
    {
        var w = new TestWorld();
        var other = new SessionTokenService(new byte[32], w.Clock);
        Assert.Equal(TokenStatus.BadSignature, w.Tokens.Validate(other.Issue("mira", 60)).Status);
    }
}
