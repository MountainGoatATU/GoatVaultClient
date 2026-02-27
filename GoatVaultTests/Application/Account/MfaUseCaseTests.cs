using GoatVaultApplication.Account;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;
using System.Text;

namespace GoatVaultTests.Application.Account;

public class MfaUseCaseTests
{
    [Fact]
    public async Task EnableMfa_WhenValid_UpdatesServerAndStoresSecretBytes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var now = DateTime.UtcNow;

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", user.AuthSalt, user.Argon2Parameters)).Returns(user.AuthVerifier);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.UpdateUserAsync(
                userId,
                It.Is<object>(o => o is EnableMfaRequest && ((EnableMfaRequest)o).MfaEnabled && ((EnableMfaRequest)o).MfaSecret == "MFA-SECRET"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(userId, mfaEnabled: true, now));

        var useCase = new EnableMfaUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object);

        // Act
        await useCase.ExecuteAsync("Password123!", "MFA-SECRET");

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u =>
            u.MfaEnabled &&
            Encoding.UTF8.GetString(u.MfaSecret) == "MFA-SECRET" &&
            u.UpdatedAtUtc == now)), Times.Once);
    }

    [Fact]
    public async Task DisableMfa_WhenValid_DisablesAndClearsSecret()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        user.MfaEnabled = true;
        user.MfaSecret = Encoding.UTF8.GetBytes("MFA-SECRET");
        var now = DateTime.UtcNow;

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", user.AuthSalt, user.Argon2Parameters)).Returns(user.AuthVerifier);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.UpdateUserAsync(
                userId,
                It.Is<object>(o => o is DisableMfaRequest && !((DisableMfaRequest)o).MfaEnabled && ((DisableMfaRequest)o).MfaSecret == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(userId, mfaEnabled: false, now));

        var useCase = new DisableMfaUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object);

        // Act
        await useCase.ExecuteAsync("Password123!");

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u => !u.MfaEnabled && u.MfaSecret.Length == 0 && u.UpdatedAtUtc == now)), Times.Once);
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
        VaultSalt = [7],
        Vault = TestFixtures.CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static UserResponse CreateUserResponse(Guid id, bool mfaEnabled, DateTime updatedAtUtc) => new()
    {
        Id = id.ToString(),
        AuthSalt = Convert.ToBase64String([1]),
        AuthVerifier = Convert.ToBase64String([2]),
        Email = "user@example.com",
        MfaEnabled = mfaEnabled,
        MfaSecret = mfaEnabled ? "MFA-SECRET" : null,
        ShamirEnabled = false,
        VaultSalt = Convert.ToBase64String([3]),
        Vault = TestFixtures.CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = updatedAtUtc
    };
}
