using GoatVaultInfrastructure.Services;

namespace GoatVaultTests.Infrastructure.Services;

public class CryptoServiceTests
{
    private readonly CryptoService _crypto = new();

    [Fact]
    public void GenerateAuthVerifier_SamePasswordAndSalt_ReturnsSameVerifier()
    {
        // Arrange
        const string password = "StrongPassword123!";
        var salt = CreateDeterministicBytes("0123456789ABCDEF0123456789ABCDEF");

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
        var salt = CreateDeterministicBytes("0123456789ABCDEF0123456789ABCDEF");

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
        var salt1 = CreateDeterministicBytes("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        var salt2 = CreateDeterministicBytes("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

        // Act
        var verifier1 = _crypto.GenerateAuthVerifier(password, salt1);
        var verifier2 = _crypto.GenerateAuthVerifier(password, salt2);

        // Assert
        Assert.NotEqual(verifier1, verifier2);
    }

    [Fact]
    public void DeriveMasterKey_SamePasswordAndSalt_ReturnsSameKeyBytes()
    {
        // Arrange
        const string password = "MasterPassword!123";
        var salt = CreateDeterministicBytes("1234567890ABCDEF1234567890ABCDEF");

        // Act
        using var key1 = _crypto.DeriveMasterKey(password, salt);
        using var key2 = _crypto.DeriveMasterKey(password, salt);

        // Assert
        Assert.Equal(key1.Key, key2.Key);
        Assert.Equal(32, key1.Key.Length);
    }

    [Fact]
    public void DeriveMasterKey_DifferentSalt_ReturnsDifferentKeyBytes()
    {
        // Arrange
        const string password = "MasterPassword!123";
        var salt1 = CreateDeterministicBytes("1234567890ABCDEF1234567890ABCDEF");
        var salt2 = CreateDeterministicBytes("ABCDEF1234567890ABCDEF1234567890");

        // Act
        using var key1 = _crypto.DeriveMasterKey(password, salt1);
        using var key2 = _crypto.DeriveMasterKey(password, salt2);

        // Assert
        Assert.NotEqual(key1.Key, key2.Key);
    }

    [Fact]
    public void DeriveMasterKey_EmptyPassword_ReturnsDeterministicKey()
    {
        // Arrange
        const string password = "";
        var salt = CreateDeterministicBytes("EMPTY-PASSWORD-SALT-0123456789012");

        // Act
        using var key1 = _crypto.DeriveMasterKey(password, salt);
        using var key2 = _crypto.DeriveMasterKey(password, salt);

        // Assert
        Assert.Equal(32, key1.Key.Length);
        Assert.Equal(key1.Key, key2.Key);
    }

    [Fact]
    public void GenerateSalt_Returns32RandomBytes()
    {
        // Act
        var salt = CryptoService.GenerateSalt();

        // Assert
        Assert.NotNull(salt);
        Assert.Equal(32, salt.Length);
        Assert.Contains(salt, b => b != 0);
    }

    [Fact]
    public void GenerateSalt_MultipleCalls_ReturnDifferentValues()
    {
        // Act
        var salt1 = CryptoService.GenerateSalt();
        var salt2 = CryptoService.GenerateSalt();
        var salt3 = CryptoService.GenerateSalt();

        // Assert
        Assert.NotEqual(salt1, salt2);
        Assert.NotEqual(salt1, salt3);
        Assert.NotEqual(salt2, salt3);
    }

    [Fact]
    public void GenerateNonce_Returns12RandomBytes()
    {
        // Act
        var nonce = CryptoService.GenerateNonce();

        // Assert
        Assert.NotNull(nonce);
        Assert.Equal(12, nonce.Length);
        Assert.Contains(nonce, b => b != 0);
    }

    [Fact]
    public void GenerateNonce_MultipleCalls_ReturnDifferentValues()
    {
        // Act
        var nonce1 = CryptoService.GenerateNonce();
        var nonce2 = CryptoService.GenerateNonce();
        var nonce3 = CryptoService.GenerateNonce();

        // Assert
        Assert.NotEqual(nonce1, nonce2);
        Assert.NotEqual(nonce1, nonce3);
        Assert.NotEqual(nonce2, nonce3);
    }

    [Fact]
    public void GenerateAuthVerifier_EmptySalt_ThrowsException()
    {
        // Arrange
        var emptySalt = Array.Empty<byte>();

        // Act + Assert
        Assert.ThrowsAny<Exception>(() => _crypto.GenerateAuthVerifier("StrongPassword123!", emptySalt));
    }

    private static byte[] CreateDeterministicBytes(string value) =>
        System.Text.Encoding.UTF8.GetBytes(value);
}
