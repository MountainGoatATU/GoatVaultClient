using GoatVaultCore.Services;

namespace GoatVaultTests.Core.Services;

public class TotpServiceTests
{
    private const string ValidSecret = "JBSWY3DPEHPK3PXP";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GenerateCodeWithTime_WhenSecretIsNullOrWhitespace_ThrowsArgumentException(string? secret)
    {
        // Assert
        Assert.Throws<ArgumentException>(() => TotpService.GenerateCodeWithTime(secret!));
    }

    [Fact]
    public void GenerateCodeWithTime_WhenSecretIsInvalid_ThrowsInvalidOperationException()
    {
        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => TotpService.GenerateCodeWithTime("%%%%"));

        // Assert
        Assert.Contains("Failed to generate TOTP code", ex.Message);
    }

    [Fact]
    public void GenerateCodeWithTime_WithValidSecret_ReturnsSixDigitCodeAndValidRemainingSeconds()
    {
        // Act
        var (code, secondsRemaining) = TotpService.GenerateCodeWithTime(ValidSecret);

        // Assert
        Assert.Matches("^\\d{6}$", code);
        Assert.InRange(secondsRemaining, 0, 30);
    }

    [Fact]
    public void VerifyCode_WithFreshGeneratedCode_ReturnsTrue()
    {
        var (code, _) = TotpService.GenerateCodeWithTime(ValidSecret);

        var result = TotpService.VerifyCode(ValidSecret, code);

        Assert.True(result);
    }

    [Fact]
    public void VerifyCode_WithWrongCode_ReturnsFalse()
    {
        // Arrange
        var (validCode, _) = TotpService.GenerateCodeWithTime(ValidSecret);
        var invalidCode = validCode == "000000" ? "000001" : "000000";

        // Act
        var result = TotpService.VerifyCode(ValidSecret, invalidCode);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null, "123456")]
    [InlineData("", "123456")]
    [InlineData("   ", "123456")]
    [InlineData(ValidSecret, null)]
    [InlineData(ValidSecret, "")]
    [InlineData(ValidSecret, "   ")]
    public void VerifyCode_WithNullOrWhitespaceInputs_ReturnsFalse(string? secret, string? code)
    {
        // Act
        var result = TotpService.VerifyCode(secret!, code!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidSecret_WhenSecretContainsFormattingCharacters_ReturnsTrue()
    {
        // Act
        var result = TotpService.IsValidSecret("jbsw y3dp-ehpk3pxp");

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("%%%%")]
    public void IsValidSecret_WhenSecretIsInvalid_ReturnsFalse(string? secret)
    {
        // Act
        var result = TotpService.IsValidSecret(secret!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GenerateSecret_DefaultLength_ReturnsValidBase32Secret()
    {
        // Act
        var secret = TotpService.GenerateSecret();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.True(TotpService.IsValidSecret(secret));
    }

    [Fact]
    public void GenerateSecret_CustomLength_ReturnsValidBase32Secret()
    {
        // Act
        var secret = TotpService.GenerateSecret(length: 10);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(secret));
        Assert.True(TotpService.IsValidSecret(secret));
    }
}
