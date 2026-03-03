using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Vault;

public class WipeVaultUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ClearsEntriesAndCategories_RaisesEvent_AndSaves()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vault = new VaultDecrypted
        {
            Categories = [new CategoryItem { Name = "Personal" }],
            Entries = [new VaultEntry { Site = "example.com", Password = "pw" }]
        };
        using var masterKey = new MasterKey(new byte[32]);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns(vault);
        session.SetupGet(x => x.UserId).Returns(userId);
        session.Setup(x => x.GetMasterKey()).Returns(masterKey);

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
            Vault = TestFixtures.CreateEncryptedVault(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };        

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Encrypt(vault, masterKey, user.VaultSalt)).Returns(TestFixtures.CreateEncryptedVault());

        var saveVault = new SaveVaultUseCase(users.Object, session.Object, vaultCrypto.Object);
        var wipeVault = new WipeVaultUseCase(session.Object, saveVault);

        // Act
        await wipeVault.ExecuteAsync();

        // Assert
        Assert.Empty(vault.Categories);
        Assert.Empty(vault.Entries);
        session.Verify(x => x.RaiseVaultChanged(), Times.Once);
        users.Verify(x => x.SaveAsync(It.Is<User>(u => ReferenceEquals(u, user))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenVaultMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns((VaultDecrypted?)null);

        var saveVault = new SaveVaultUseCase(Mock.Of<IUserRepository>(), session.Object, Mock.Of<IVaultCrypto>());
        var wipeVault = new WipeVaultUseCase(session.Object, saveVault);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await wipeVault.ExecuteAsync());
    }
}
