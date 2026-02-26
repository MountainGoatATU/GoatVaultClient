using GoatVaultApplication.Shamir;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Shamir;

public class EnableDisableShamirUseCaseTests
{
    [Fact]
    public async Task Enable_WhenNoUserLoggedIn_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns((Guid?)null);
        var useCase = new EnableShamirUseCase(session.Object, Mock.Of<IUserRepository>(), Mock.Of<ICryptoService>(), Mock.Of<IServerAuthService>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync("Password123!"));
    }

    [Fact]
    public async Task Enable_WhenPasswordInvalid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, shamirEnabled: false);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("wrong", user.AuthSalt)).Returns([9, 9, 9]);

        var useCase = new EnableShamirUseCase(session.Object, users.Object, crypto.Object, Mock.Of<IServerAuthService>());

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await useCase.ExecuteAsync("wrong"));
    }

    [Fact]
    public async Task Enable_WhenValid_UpdatesServerAndSavesLocalUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, shamirEnabled: false);
        var previousUpdatedAt = user.UpdatedAtUtc;
        var now = DateTime.UtcNow;

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", user.AuthSalt)).Returns(user.AuthVerifier);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.UpdateUserAsync(
                userId,
                It.Is<object>(o => o is DisableShamirRequest && ((DisableShamirRequest)o).ShamirEnabled),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateServerUserResponse(userId, shamirEnabled: true, updatedAtUtc: now));

        var useCase = new EnableShamirUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object);

        // Act
        await useCase.ExecuteAsync("Password123!");

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u =>
            u.ShamirEnabled &&
            u.UpdatedAtUtc == now &&
            u.UpdatedAtUtc > previousUpdatedAt)), Times.Once);
    }

    [Fact]
    public async Task Disable_WhenAlreadyDisabled_ThrowsInvalidOperationException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, shamirEnabled: false);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var useCase = new DisableShamirUseCase(session.Object, users.Object, Mock.Of<ICryptoService>(), Mock.Of<IServerAuthService>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync("Password123!"));
    }

    [Fact]
    public async Task Disable_WhenValid_UpdatesServerAndSavesLocalUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId, shamirEnabled: true);
        var previousUpdatedAt = user.UpdatedAtUtc;
        var now = DateTime.UtcNow;

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", user.AuthSalt)).Returns(user.AuthVerifier);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.UpdateUserAsync(
                userId,
                It.Is<object>(o => o is DisableShamirRequest && !((DisableShamirRequest)o).ShamirEnabled),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateServerUserResponse(userId, shamirEnabled: false, updatedAtUtc: now));

        var useCase = new DisableShamirUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object);

        // Act
        await useCase.ExecuteAsync("Password123!");

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u =>
            !u.ShamirEnabled &&
            u.UpdatedAtUtc == now &&
            u.UpdatedAtUtc > previousUpdatedAt)), Times.Once);
    }

    private static User CreateUser(Guid id, bool shamirEnabled) => new()
    {
        Id = id,
        Email = new Email("user@example.com"),
        AuthSalt = [1, 2, 3],
        AuthVerifier = [4, 5, 6],
        MfaEnabled = false,
        MfaSecret = [],
        ShamirEnabled = shamirEnabled,
        VaultSalt = [7, 8],
        Vault = CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static UserResponse CreateServerUserResponse(Guid id, bool shamirEnabled, DateTime updatedAtUtc) => new()
    {
        Id = id.ToString(),
        AuthSalt = Convert.ToBase64String([1, 2, 3]),
        AuthVerifier = Convert.ToBase64String([4, 5, 6]),
        Email = "user@example.com",
        MfaEnabled = false,
        MfaSecret = null,
        ShamirEnabled = shamirEnabled,
        VaultSalt = Convert.ToBase64String([7, 8]),
        Vault = CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = updatedAtUtc
    };

    private static VaultEncrypted CreateEncryptedVault() => new(
        encryptedBlob: [1, 2, 3],
        nonce: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
        authTag: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
    {
        EncryptedBlob = [1, 2, 3],
        Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
        AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
    };
}
