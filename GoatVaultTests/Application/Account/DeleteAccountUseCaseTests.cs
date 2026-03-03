using GoatVaultApplication.Account;
using GoatVaultApplication.Auth;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Account;

public class DeleteAccountUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPasswordInvalid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("wrong", user.AuthSalt, user.Argon2Parameters)).Returns([9, 9, 9]);

        var logout = new LogoutUseCase(session.Object, Mock.Of<ISyncingService>());
        var useCase = new DeleteAccountUseCase(
            session.Object,
            users.Object,
            crypto.Object,
            Mock.Of<IServerAuthService>(),
            logout);

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await useCase.ExecuteAsync("wrong", ct));
    }

    [Fact]
    public async Task ExecuteAsync_WhenGetUserThrows_AssumesDeleteSucceededAndLogsOut()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);
        var syncing = new Mock<ISyncingService>();

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", user.AuthSalt, user.Argon2Parameters)).Returns(user.AuthVerifier);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.DeleteUserAsync(userId, ct)).ReturnsAsync("deleted");
        serverAuth.Setup(x => x.GetUserAsync(userId, ct)).ThrowsAsync(new Exception("not found"));

        var logout = new LogoutUseCase(session.Object, syncing.Object);

        var useCase = new DeleteAccountUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object, logout);

        // Act
        await useCase.ExecuteAsync("Password123!", ct);

        // Assert
        users.Verify(x => x.DeleteAsync(user), Times.Once);
        syncing.Verify(x => x.StopPeriodicSync(), Times.Once);
        session.Verify(x => x.End(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenGetUserReturnsUser_ThrowsInvalidOperationException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);
        var syncing = new Mock<ISyncingService>();

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", user.AuthSalt, user.Argon2Parameters)).Returns(user.AuthVerifier);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.DeleteUserAsync(userId, ct)).ReturnsAsync("deleted");
        serverAuth.Setup(x => x.GetUserAsync(userId, ct)).ReturnsAsync(new UserResponse
        {
            Id = userId.ToString(),
            AuthSalt = Convert.ToBase64String(user.AuthSalt),
            AuthVerifier = Convert.ToBase64String(user.AuthVerifier),
            Email = user.Email.Value,
            MfaEnabled = user.MfaEnabled,
            MfaSecret = null,
            ShamirEnabled = user.ShamirEnabled,
            VaultSalt = Convert.ToBase64String(user.VaultSalt),
            Argon2Parameters = user.Argon2Parameters,
            Vault = user.Vault,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        });

        var logout = new LogoutUseCase(session.Object, syncing.Object);

        var useCase = new DeleteAccountUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object, logout);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await useCase.ExecuteAsync("Password123!", ct));

        users.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Never);
        syncing.Verify(x => x.StopPeriodicSync(), Times.Never);
        session.Verify(x => x.End(), Times.Never);
    }

    private static User CreateUser(Guid id) => new()
    {
        Id = id,
        Email = new Email("user@example.com"),
        AuthSalt = [1, 2, 3],
        AuthVerifier = [4, 5, 6],
        MfaEnabled = false,
        MfaSecret = [],
        ShamirEnabled = false,
        VaultSalt = [7, 8],
        Vault = TestFixtures.CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
