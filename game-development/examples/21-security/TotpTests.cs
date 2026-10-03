using System.Text;

namespace Course.Security;

public class TotpTests
{
    // RFC 6238 Appendix B: SHA-1 secret is the ASCII string "12345678901234567890", 8 digits, 30 s step.
    private static readonly byte[] Secret = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1111111111L, "14050471")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(2000000000L, "69279037")]
    [InlineData(20000000000L, "65353130")]
    public void Generate_Rfc6238Sha1Vectors_MatchExactly(long unixSeconds, string expected) =>
        Assert.Equal(expected, Totp.Generate(Secret, unixSeconds, digits: 8));

    [Fact]
    public void Generate_SixDigits_IsLastSixOfTheEightDigitCode() =>
        Assert.Equal("287082", Totp.Generate(Secret, 59, digits: 6));   // 94287082 -> 287082

    [Fact]
    public void Generate_SameStep_SameCode() =>
        Assert.Equal(Totp.Generate(Secret, 60), Totp.Generate(Secret, 89));   // 60..89 is one 30 s step

    [Fact]
    public void Verify_CorrectCode_Valid()
    {
        var v = new TotpVerifier();
        var code = Totp.Generate(Secret, 1_000_000);
        Assert.Equal(TotpResult.Valid, v.Verify("a", Secret, code, 1_000_000));
    }

    [Fact]
    public void Verify_SameCodeTwice_SecondIsAlreadyUsed()
    {
        var v = new TotpVerifier();
        var code = Totp.Generate(Secret, 1_000_000);
        v.Verify("a", Secret, code, 1_000_000);
        Assert.Equal(TotpResult.AlreadyUsed, v.Verify("a", Secret, code, 1_000_005));
    }

    [Fact]
    public void Verify_PreviousStepCode_AcceptedWithinDriftButNotTwo()
    {
        var v = new TotpVerifier(driftSteps: 1);
        var oneStepOld = Totp.Generate(Secret, 1_000_000 - 30);
        var twoStepsOld = Totp.Generate(Secret, 1_000_000 - 60);

        Assert.Equal(TotpResult.Invalid, v.Verify("a", Secret, twoStepsOld, 1_000_000));
        Assert.Equal(TotpResult.Valid, v.Verify("a", Secret, oneStepOld, 1_000_000));
    }

    [Fact]
    public void Verify_FiveWrongCodes_LocksOutEvenTheRightCode()
    {
        var v = new TotpVerifier(maxFailures: 5);
        for (var i = 0; i < 5; i++)
            Assert.Equal(TotpResult.Invalid, v.Verify("a", Secret, "000000", 1_000_000));

        var right = Totp.Generate(Secret, 1_000_000);
        Assert.Equal(TotpResult.LockedOut, v.Verify("a", Secret, right, 1_000_000));
    }

    [Fact]
    public void Verify_SuccessResetsTheFailureCount()
    {
        var v = new TotpVerifier(maxFailures: 3);
        v.Verify("a", Secret, "000000", 1_000_000);
        v.Verify("a", Secret, "000000", 1_000_000);
        Assert.Equal(TotpResult.Valid, v.Verify("a", Secret, Totp.Generate(Secret, 1_000_000), 1_000_000));
        v.Verify("a", Secret, "000000", 1_000_030);
        v.Verify("a", Secret, "000000", 1_000_030);
        Assert.Equal(TotpResult.Valid, v.Verify("a", Secret, Totp.Generate(Secret, 1_000_060), 1_000_060));
    }
}
