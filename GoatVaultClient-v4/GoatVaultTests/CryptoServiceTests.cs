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
}