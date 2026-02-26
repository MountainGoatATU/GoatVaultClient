using GoatVaultApplication.Auth;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Auth;

public class LoginOfflineUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenLocalUserMissing_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);

        var useCase = new LoginOfflineUseCase(
            users.Object,
            Mock.Of<ICryptoService>(),
            Mock.Of<IVaultCrypto>(),
            Mock.Of<ISessionContext>(),
            Mock.Of<IPasswordStrengthService>());

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await useCase.ExecuteAsync(Guid.NewGuid(), "password"));
    }

    [Fact]
    public async Task ExecuteAsync_WithValidLocalUser_DecryptsVaultAndStartsSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        using var masterKey = new MasterKey(new byte[32]);
        var decryptedVault = new VaultDecrypted { Categories = [], Entries = [] };

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.DeriveMasterKey("Password123!", user.VaultSalt)).Returns(masterKey);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Decrypt(user.Vault, masterKey)).Returns(decryptedVault);

        var passwordStrength = new Mock<IPasswordStrengthService>();
        passwordStrength.Setup(x => x.Evaluate("Password123!")).Returns(new PasswordStrength { Score = 3 });

        var session = new Mock<ISessionContext>();

        var useCase = new LoginOfflineUseCase(
            users.Object,
            crypto.Object,
            vaultCrypto.Object,
            session.Object,
            passwordStrength.Object);

        // Act
        await useCase.ExecuteAsync(userId, "Password123!");

        // Assert
        session.Verify(x => x.Start(userId, masterKey, decryptedVault, 3), Times.Once);
    }

    private static User CreateUser(Guid id)
    {
        var vault = new VaultEncrypted(
            encryptedBlob: [1, 2, 3],
            nonce: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            authTag: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
        {
            EncryptedBlob = [1, 2, 3],
            Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
        };

        return new User
        {
            Id = id,
            Email = new Email("offline@example.com"),
            AuthSalt = [1, 2, 3],
            AuthVerifier = [4, 5, 6],
            MfaEnabled = false,
            MfaSecret = [],
            ShamirEnabled = false,
            VaultSalt = [7, 8, 9],
            Vault = vault,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }
}
