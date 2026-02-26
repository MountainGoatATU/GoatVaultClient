using GoatVaultInfrastructure.Services;

namespace GoatVaultTests;

public class CryptoServiceTests
{
    private readonly CryptoService _crypto = new();
    private static byte[] CreateDeterministicSalt() =>
        // 16 bytes, deterministic
        "0123456789ABCDEF"u8.ToArray();

    [Fact]
    public void GenerateAuthVerifier_SamePasswordAndSalt_ReturnsSameVerifier()
    {
        // Arrange
        const string password = "StrongPassword123!";
        var salt = CreateDeterministicSalt();

        // Act
        var verifier1 = _crypto.GenerateAuthVerifier(password, salt);
        var verifier2 = _crypto.GenerateAuthVerifier(password, salt);

        // Assert
        Assert.Equal(verifier1, verifier2);
    }

    [Fact]
    public void GenerateAuthVerifier_DifferentPassword_ReturnsDifferentVerifier()
    {
        // Arrange
        var salt = CreateDeterministicSalt();

        // Act
        var verifier1 = _crypto.GenerateAuthVerifier("PasswordOne!", salt);
        var verifier2 = _crypto.GenerateAuthVerifier("PasswordTwo!", salt);

        // Assert
        Assert.NotEqual(verifier1, verifier2);
    }

    [Fact]
    public void GenerateAuthVerifier_DifferentSalt_ReturnsDifferentVerifier()
    {
        // Arrange
        const string password = "StrongPassword123!";

        var salt1 = "AAAAAAAAAAAAAAAA"u8.ToArray();
        var salt2 = "BBBBBBBBBBBBBBBB"u8.ToArray();

        // Act
        var verifier1 = _crypto.GenerateAuthVerifier(password, salt1);
        var verifier2 = _crypto.GenerateAuthVerifier(password, salt2);

        // Assert
        Assert.NotEqual(verifier1, verifier2);
    }

    [Fact]
    public void GenerateSalt_Returns32RandomBytes()
    {
        // Act
        var salt = CryptoService.GenerateSalt();

        // Assert
        Assert.NotNull(salt);
        Assert.Equal(32, salt.Length);
    }

    [Fact]
    public void GenerateNonce_Returns12RandomBytes()
    {
        // Act
        var nonce = CryptoService.GenerateNonce();

        // Assert
        Assert.NotNull(nonce);
        Assert.Equal(12, nonce.Length);
    }

    [Fact]
    public void GenerateSalt_ShouldNotReturnAllZeros()
    {
        // Act
        var bytes = CryptoService.GenerateSalt();

        // Assert
        Assert.Contains(bytes, b => b != 0);
    }
    /*

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
        var invalidSalt = "Not-Valid-Base64!!!"u8.ToArray();

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _crypto.GenerateAuthVerifier(password, invalidSalt));
    }
    */
}