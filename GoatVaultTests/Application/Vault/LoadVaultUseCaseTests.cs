using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Vault;

public class LoadVaultUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenUserIdMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns((Guid?)null);
        session.Setup(x => x.GetMasterKey()).Returns(new MasterKey(new byte[32]));

        var useCase = new LoadVaultUseCase(Mock.Of<IUserRepository>(), session.Object, Mock.Of<IVaultCrypto>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);
        session.Setup(x => x.GetMasterKey()).Returns(new MasterKey(new byte[32]));

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var useCase = new LoadVaultUseCase(users.Object, session.Object, Mock.Of<IVaultCrypto>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_DecryptsVaultUpdatesSessionAndReturnsVault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        using var masterKey = new MasterKey(new byte[32]);
        var encrypted = TestFixtures.CreateEncryptedVault();
        var decrypted = new VaultDecrypted { Categories = [], Entries = [] };
        var user = new User
        {
            Id = userId,
            Email = new Email("user@example.com"),
            AuthSalt = [1],
            AuthVerifier = [2],
            MfaEnabled = false,
            MfaSecret = [],
            ShamirEnabled = false,
            VaultSalt = [3],
            Vault = encrypted,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);
        session.Setup(x => x.GetMasterKey()).Returns(masterKey);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<IVaultCrypto>();
        crypto.Setup(x => x.Decrypt(encrypted, masterKey)).Returns(decrypted);

        var useCase = new LoadVaultUseCase(users.Object, session.Object, crypto.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Same(decrypted, result);
        session.Verify(x => x.UpdateVault(decrypted), Times.Once);
    }

}
