using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Vault;

public class SaveVaultUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenVaultMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns((VaultDecrypted?)null);

        var useCase = new SaveVaultUseCase(Mock.Of<IUserRepository>(), session.Object, Mock.Of<IVaultCrypto>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var masterKey = new MasterKey(new byte[32]);
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns(new VaultDecrypted { Categories = [], Entries = [] });
        session.SetupGet(x => x.UserId).Returns(userId);
        session.Setup(x => x.GetMasterKey()).Returns(masterKey);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var useCase = new SaveVaultUseCase(users.Object, session.Object, Mock.Of<IVaultCrypto>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_EncryptsVaultAndSavesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var masterKey = new MasterKey(new byte[32]);
        var vault = new VaultDecrypted { Categories = [], Entries = [] };
        var user = CreateUser(userId);
        var encrypted = TestFixtures.CreateEncryptedVault();

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns(vault);
        session.SetupGet(x => x.UserId).Returns(userId);
        session.Setup(x => x.GetMasterKey()).Returns(masterKey);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<IVaultCrypto>();
        crypto.Setup(x => x.Encrypt(vault, masterKey, user.VaultSalt)).Returns(encrypted);

        var before = DateTime.UtcNow;
        var useCase = new SaveVaultUseCase(users.Object, session.Object, crypto.Object);

        // Act
        await useCase.ExecuteAsync();

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u => ReferenceEquals(u, user) && ReferenceEquals(u.Vault, encrypted) && u.UpdatedAtUtc >= before)), Times.Once);
    }

    private static User CreateUser(Guid id) => new()
    {
        Id = id,
        Email = new Email("user@example.com"),
        AuthSalt = [1],
        AuthVerifier = [2],
        MfaEnabled = false,
        MfaSecret = [],
        ShamirEnabled = false,
        VaultSalt = [7, 8, 9],
        Vault = TestFixtures.CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
