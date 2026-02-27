using GoatVaultApplication.Auth;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Auth;

public class RegisterUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPasswordBreached_ThrowsAndDoesNotCallRegister()
    {
        // Arrange
        var crypto = new Mock<ICryptoService>();
        var vaultCrypto = new Mock<IVaultCrypto>();
        var serverAuth = new Mock<IServerAuthService>();
        var pwned = new Mock<IPwnedPasswordService>();
        pwned.Setup(x => x.CheckPasswordAsync("weak")).ReturnsAsync(1);

        var registerUseCase = new RegisterUseCase(crypto.Object, vaultCrypto.Object, serverAuth.Object, pwned.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await registerUseCase.ExecuteAsync(new Email("user@example.com"), "weak"));

        serverAuth.Verify(x => x.RegisterAsync(It.IsAny<AuthRegisterRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPasswordSafe_BuildsPayloadAndCallsRegister()
    {
        // Arrange
        var authSalt = new byte[] { 1, 2, 3, 4 };
        var vaultSalt = new byte[] { 9, 8, 7, 6 };
        var authVerifier = new byte[] { 10, 11, 12 };
        using var masterKey = new MasterKey(new byte[32]);
        var encryptedVault = new VaultEncrypted(
            encryptedBlob: [1, 2, 3],
            nonce: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            authTag: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
        {
            EncryptedBlob = [1, 2, 3],
            Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
        };

        var crypto = new Mock<ICryptoService>();
        crypto.SetupSequence(x => x.GenerateSalt())
            .Returns(authSalt)
            .Returns(vaultSalt);
        crypto.Setup(x => x.GenerateAuthVerifier("StrongPassword123!", authSalt, It.IsAny<Argon2Parameters?>())).Returns(authVerifier);
        crypto.Setup(x => x.DeriveMasterKey("StrongPassword123!", vaultSalt, It.IsAny<Argon2Parameters?>())).Returns(masterKey);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Encrypt(It.IsAny<VaultDecrypted>(), masterKey, vaultSalt)).Returns(encryptedVault);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.RegisterAsync(It.IsAny<AuthRegisterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthRegisterResponse { Id = Guid.NewGuid().ToString(), Email = "user@example.com" });

        var pwned = new Mock<IPwnedPasswordService>();
        pwned.Setup(x => x.CheckPasswordAsync("StrongPassword123!")).ReturnsAsync(0);

        var registerUseCase = new RegisterUseCase(crypto.Object, vaultCrypto.Object, serverAuth.Object, pwned.Object);

        // Act
        await registerUseCase.ExecuteAsync(new Email("user@example.com"), "StrongPassword123!");

        // Assert
        serverAuth.Verify(x => x.RegisterAsync(
            It.Is<AuthRegisterRequest>(r =>
                r.Email == "user@example.com" &&
                r.AuthSalt == Convert.ToBase64String(authSalt) &&
                r.AuthVerifier == Convert.ToBase64String(authVerifier) &&
                r.VaultSalt == Convert.ToBase64String(vaultSalt) &&
                ReferenceEquals(r.Vault, encryptedVault)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WhenBreachApiFails_ReturnsWarningResult()
    {
        // Arrange
        var useCase = CreateUseCaseForValidation(passwordResponse: null);

        // Act
        var result = await useCase.ValidatePasswordAsync("ValidLengthPassword1!");

        // Assert
        Assert.True(result.IsWarning);
        Assert.False(result.IsGood);
        Assert.Contains("Unable to reach breach database", result.Message);
    }

    [Fact]
    public async Task ValidatePasswordAsync_WhenPasswordIsSafe_ReturnsGoodResult()
    {
        // Arrange
        var useCase = CreateUseCaseForValidation(passwordResponse: 0);

        // Act
        var result = await useCase.ValidatePasswordAsync("ValidLengthPassword1!");

        // Assert
        Assert.False(result.IsWarning);
        Assert.True(result.IsGood);
    }

    [Theory]
    [InlineData("bad-email")]
    [InlineData("missing@domain")]
    public void ValidateEmail_InvalidFormat_ReturnsWarning(string email)
    {
        // Arrange
        var useCase = CreateUseCaseForValidation(passwordResponse: 0);

        // Act
        var result = useCase.ValidateEmail(email);

        // Assert
        Assert.True(result.IsWarning);
        Assert.False(result.IsGood);
    }

    private static RegisterUseCase CreateUseCaseForValidation(int? passwordResponse)
    {
        var crypto = new Mock<ICryptoService>();
        var vaultCrypto = new Mock<IVaultCrypto>();
        var serverAuth = new Mock<IServerAuthService>();
        var pwned = new Mock<IPwnedPasswordService>();
        pwned.Setup(x => x.CheckPasswordAsync(It.IsAny<string>())).ReturnsAsync(passwordResponse);
        return new RegisterUseCase(crypto.Object, vaultCrypto.Object, serverAuth.Object, pwned.Object);
    }
}
