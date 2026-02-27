using GoatVaultApplication.Account;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Account;

public class ChangeEmailUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPasswordIncorrect_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("wrong", user.AuthSalt, user.Argon2Parameters)).Returns([9, 9, 9]);

        var useCase = new ChangeEmailUseCase(session.Object, users.Object, crypto.Object, Mock.Of<IServerAuthService>());

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await useCase.ExecuteAsync("wrong", new Email("new@example.com")));
    }

    [Fact]
    public async Task ExecuteAsync_WhenValid_UpdatesServerAndLocalUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var newEmail = new Email("new@example.com");
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
                It.Is<object>(o => o is ChangeEmailRequest && ((ChangeEmailRequest)o).Email == "new@example.com"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(userId, now));

        var useCase = new ChangeEmailUseCase(session.Object, users.Object, crypto.Object, serverAuth.Object);

        // Act
        await useCase.ExecuteAsync("Password123!", newEmail);

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u => u.Email.Value == "new@example.com" && u.UpdatedAtUtc == now)), Times.Once);
    }

    private static User CreateUser(Guid id) => new()
    {
        Id = id,
        Email = new Email("old@example.com"),
        AuthSalt = [1, 2, 3],
        AuthVerifier = [4, 5, 6],
        MfaEnabled = false,
        MfaSecret = [],
        ShamirEnabled = false,
        VaultSalt = [7],
        Vault = CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static UserResponse CreateUserResponse(Guid id, DateTime updatedAtUtc) => new()
    {
        Id = id.ToString(),
        AuthSalt = Convert.ToBase64String([1]),
        AuthVerifier = Convert.ToBase64String([2]),
        Email = "new@example.com",
        MfaEnabled = false,
        MfaSecret = null,
        ShamirEnabled = false,
        VaultSalt = Convert.ToBase64String([3]),
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
