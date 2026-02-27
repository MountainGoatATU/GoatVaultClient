using GoatVaultApplication.Account;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;

namespace GoatVaultTests.Application.Account;

public class ChangePasswordUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCurrentPasswordInvalid_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("wrong", user.AuthSalt)).Returns([9, 9, 9]);

        var useCase = new ChangePasswordUseCase(
            session.Object,
            users.Object,
            crypto.Object,
            Mock.Of<IVaultCrypto>(),
            Mock.Of<IServerAuthService>(),
            Mock.Of<IPasswordStrengthService>());

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await useCase.ExecuteAsync("wrong", "NewPassword123!"));
    }

    [Fact]
    public async Task ExecuteAsync_WhenValid_UpdatesServerLocalUserAndSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateUser(userId);
        var currentVerifier = user.AuthVerifier;
        var newVerifier = new byte[] { 40, 41, 42, 43 };
        var decryptedVault = new VaultDecrypted { Categories = [], Entries = [] };
        var newEncryptedVault = TestFixtures.CreateEncryptedVault();
        using var currentMasterKey = new MasterKey(new byte[32]);
        using var newMasterKey = new MasterKey(new byte[32]);
        var updatedAt = DateTime.UtcNow;

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(user);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("CurrentPassword123!", user.AuthSalt)).Returns(currentVerifier);
        crypto.Setup(x => x.GenerateAuthVerifier("NewPassword123!", It.IsAny<byte[]>())).Returns(newVerifier);
        crypto.Setup(x => x.DeriveMasterKey("CurrentPassword123!", user.VaultSalt)).Returns(currentMasterKey);
        crypto.Setup(x => x.DeriveMasterKey("NewPassword123!", It.IsAny<byte[]>())).Returns(newMasterKey);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Decrypt(user.Vault, currentMasterKey)).Returns(decryptedVault);
        vaultCrypto.Setup(x => x.Encrypt(decryptedVault, newMasterKey, It.IsAny<byte[]>())).Returns(newEncryptedVault);

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.UpdateUserAsync(
                userId,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUserResponse(userId, updatedAt));

        var passwordStrength = new Mock<IPasswordStrengthService>();
        passwordStrength.Setup(x => x.Evaluate("NewPassword123!")).Returns(new PasswordStrength { Score = 4 });

        var useCase = new ChangePasswordUseCase(
            session.Object,
            users.Object,
            crypto.Object,
            vaultCrypto.Object,
            serverAuth.Object,
            passwordStrength.Object);

        // Act
        await useCase.ExecuteAsync("CurrentPassword123!", "NewPassword123!");

        // Assert
        serverAuth.Verify(x => x.UpdateUserAsync(
            userId,
            It.Is<object>(o =>
                o is ChangeMasterPasswordRequest &&
                ((ChangeMasterPasswordRequest)o).AuthVerifier == Convert.ToBase64String(newVerifier) &&
                ((ChangeMasterPasswordRequest)o).AuthSalt != ((ChangeMasterPasswordRequest)o).AuthVerifier &&
                Convert.FromBase64String(((ChangeMasterPasswordRequest)o).AuthSalt).Length == 32 &&
                Convert.FromBase64String(((ChangeMasterPasswordRequest)o).VaultSalt).Length == 32 &&
                ReferenceEquals(((ChangeMasterPasswordRequest)o).Vault, newEncryptedVault)),
            It.IsAny<CancellationToken>()),
            Times.Once);

        users.Verify(x => x.SaveAsync(It.Is<User>(u =>
            u.AuthVerifier.SequenceEqual(newVerifier) &&
            ReferenceEquals(u.Vault, newEncryptedVault) &&
            u.UpdatedAtUtc == updatedAt)), Times.Once);

        session.Verify(x => x.Start(userId, newMasterKey, decryptedVault, 4), Times.Once);
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
        VaultSalt = [7, 8, 9],
        Vault = TestFixtures.CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };

    private static UserResponse CreateUserResponse(Guid id, DateTime updatedAtUtc) => new()
    {
        Id = id.ToString(),
        AuthSalt = Convert.ToBase64String([1]),
        AuthVerifier = Convert.ToBase64String([2]),
        Email = "user@example.com",
        MfaEnabled = false,
        MfaSecret = null,
        ShamirEnabled = false,
        VaultSalt = Convert.ToBase64String([3]),
        Vault = TestFixtures.CreateEncryptedVault(),
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = updatedAtUtc
    };
}
