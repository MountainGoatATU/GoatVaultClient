using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Vault;

public class EntryVaultUseCasesTests
{
    [Fact]
    public async Task AddVaultEntryUseCase_AddsEntryRaisesEventAndSaves()
    {
        // Arrange
        var context = CreateEntryContext();
        var useCase = new AddVaultEntryUseCase(context.Session.Object, context.SaveUseCase);
        var newEntry = new VaultEntry { Site = "new.example", Password = "pw" };

        // Act
        await useCase.ExecuteAsync(newEntry);

        // Assert
        Assert.Contains(newEntry, context.Vault.Entries);
        context.Session.Verify(x => x.RaiseVaultChanged(), Times.Once);
        context.Users.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task DeleteVaultEntryUseCase_RemovesEntryRaisesEventAndSaves()
    {
        // Arrange
        var context = CreateEntryContext();
        var existing = context.Vault.Entries[0];
        var useCase = new DeleteVaultEntryUseCase(context.Session.Object, context.SaveUseCase);

        // Act
        await useCase.ExecuteAsync(existing);

        // Assert
        Assert.DoesNotContain(existing, context.Vault.Entries);
        context.Session.Verify(x => x.RaiseVaultChanged(), Times.Once);
        context.Users.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateVaultEntryUseCase_ReplacesExistingEntryRaisesEventAndSaves()
    {
        // Arrange
        var context = CreateEntryContext();
        var oldEntry = context.Vault.Entries[0];
        var newEntry = new VaultEntry { Site = "updated.example", Password = "new-pw" };
        var useCase = new UpdateVaultEntryUseCase(context.Session.Object, context.SaveUseCase);

        // Act
        await useCase.ExecuteAsync(oldEntry, newEntry);

        // Assert
        Assert.DoesNotContain(oldEntry, context.Vault.Entries);
        Assert.Contains(newEntry, context.Vault.Entries);
        context.Session.Verify(x => x.RaiseVaultChanged(), Times.Once);
        context.Users.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateVaultEntryUseCase_WhenOldEntryMissing_AddsNewEntry()
    {
        // Arrange
        var context = CreateEntryContext();
        var oldEntry = new VaultEntry { Site = "missing", Password = "x" };
        var newEntry = new VaultEntry { Site = "added.example", Password = "new-pw" };
        var useCase = new UpdateVaultEntryUseCase(context.Session.Object, context.SaveUseCase);

        // Act
        await useCase.ExecuteAsync(oldEntry, newEntry);

        // Assert
        Assert.Contains(newEntry, context.Vault.Entries);
        context.Users.Verify(x => x.SaveAsync(It.IsAny<User>()), Times.Once);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("delete")]
    [InlineData("update")]
    public async Task EntryUseCases_WhenVaultMissing_ThrowInvalidOperationException(string mode)
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns((VaultDecrypted?)null);
        var saveUseCase = new SaveVaultUseCase(Mock.Of<IUserRepository>(), session.Object, Mock.Of<IVaultCrypto>());

        // Act + Assert
        if (mode == "add")
        {
            var useCase = new AddVaultEntryUseCase(session.Object, saveUseCase);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(new VaultEntry()));
        }
        else if (mode == "delete")
        {
            var useCase = new DeleteVaultEntryUseCase(session.Object, saveUseCase);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(new VaultEntry()));
        }
        else
        {
            var useCase = new UpdateVaultEntryUseCase(session.Object, saveUseCase);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync(new VaultEntry(), new VaultEntry()));
        }
    }

    private static EntryContext CreateEntryContext()
    {
        var userId = Guid.NewGuid();
        using var _ = new MasterKey(new byte[32]);
        var vault = new VaultDecrypted
        {
            Categories = [],
            Entries = [new VaultEntry { Site = "old.example", Password = "old-pw" }]
        };

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.Vault).Returns(vault);
        session.SetupGet(x => x.UserId).Returns(userId);
        session.Setup(x => x.GetMasterKey()).Returns(new MasterKey(new byte[32]));

        var user = new User
        {
            Id = userId,
            Email = new Email("user@example.com"),
            AuthSalt = [1],
            AuthVerifier = [2],
            MfaEnabled = false,
            MfaSecret = [],
            ShamirEnabled = false,
            VaultSalt = [3, 4, 5],
            Vault = TestFixtures.CreateEncryptedVault(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<IVaultCrypto>();
        crypto.Setup(x => x.Encrypt(vault, It.IsAny<MasterKey>(), user.VaultSalt)).Returns(TestFixtures.CreateEncryptedVault());

        var saveUseCase = new SaveVaultUseCase(users.Object, session.Object, crypto.Object);
        return new EntryContext(vault, session, users, saveUseCase);
    }

    private sealed record EntryContext(
        VaultDecrypted Vault,
        Mock<ISessionContext> Session,
        Mock<IUserRepository> Users,
        SaveVaultUseCase SaveUseCase);
}
