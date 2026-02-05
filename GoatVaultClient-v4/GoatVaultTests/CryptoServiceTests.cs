using GoatVaultCore.Services.Secrets;

namespace GoatVaultTests;

public class CryptoServiceTests
{
    private static string CreateDeterministicBase64Salt()
    {
        // 16 bytes, deterministic, valid Base64
        var saltBytes = "0123456789ABCDEF"u8.ToArray();
        return Convert.ToBase64String(saltBytes);
    }

    [Fact]
    public void GenerateAuthVerifier_SamePasswordAndSalt_ReturnsSameVerifier()
    {
        // Arrange
        const string password = "StrongPassword123!";
        var saltBase64 = CreateDeterministicBase64Salt();

        // Act
        var verifier1 = CryptoService.GenerateAuthVerifier(password, saltBase64);
        var verifier2 = CryptoService.GenerateAuthVerifier(password, saltBase64);

        // Assert
        Assert.Equal(verifier1, verifier2);
    }

    [Fact]
    public void GenerateAuthVerifier_DifferentPassword_ReturnsDifferentVerifier()
    {
        // Arrange
        var saltBase64 = CreateDeterministicBase64Salt();

        // Act
        var verifier1 = CryptoService.GenerateAuthVerifier("PasswordOne!", saltBase64);
        var verifier2 = CryptoService.GenerateAuthVerifier("PasswordTwo!", saltBase64);

        // Assert
        Assert.NotEqual(verifier1, verifier2);
    }

    [Fact]
    public void GenerateAuthVerifier_DifferentSalt_ReturnsDifferentVerifier()
    {
        // Arrange
        const string password = "StrongPassword123!";

        var salt1 = Convert.ToBase64String("AAAAAAAAAAAAAAAA"u8.ToArray());
        var salt2 = Convert.ToBase64String("BBBBBBBBBBBBBBBB"u8.ToArray());

        // Act
        var verifier1 = CryptoService.GenerateAuthVerifier(password, salt1);
        var verifier2 = CryptoService.GenerateAuthVerifier(password, salt2);

        // Assert
        Assert.NotEqual(verifier1, verifier2);
    }

    [Fact]
    public void GenerateAuthSalt_Returns16RandomBytes()
    {
        // Act
        var salt = CryptoService.GenerateAuthSalt();

        // Assert
        Assert.NotNull(salt);
        Assert.Equal(16, salt.Length);
    }

    [Fact]
    public void GenerateAuthSalt_MultipleCalls_ReturnsDifferentSalts()
    {
        // Act
        var salt1 = CryptoService.GenerateAuthSalt();
        var salt2 = CryptoService.GenerateAuthSalt();
        var salt3 = CryptoService.GenerateAuthSalt();

        // Assert
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(salt2, salt3);
        Assert.NotEqual(salt1, salt3);
    }

    [Fact]
    public void GenerateAuthVerifier_WithInvalidBase64Salt_ThrowsException()
    {
        // Arrange
        const string password = "ValidPassword123!";
        const string invalidSalt = "Not-Valid-Base64!!!";

        // Act & Assert
        Assert.ThrowsAny<Exception>(() =>
            CryptoService.GenerateAuthVerifier(password, invalidSalt));
    }

    [Fact]
    public void GenerateAuthVerifier_ReturnsBase64EncodedString()
    {
        // Arrange
        const string password = "TestPassword123!";
        var salt = CreateDeterministicBase64Salt();

        // Act
        var verifier = CryptoService.GenerateAuthVerifier(password, salt);

        // Assert
        Assert.NotEmpty(verifier);

        // Should be valid Base64
        var decoded = Convert.FromBase64String(verifier);
        Assert.NotEmpty(decoded);
    }
}