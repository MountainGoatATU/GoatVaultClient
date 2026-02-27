using GoatVaultApplication.Auth;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using Moq;
using System.Security.Cryptography;

namespace GoatVaultTests.Application.Auth;

public class LoginOnlineUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WithMfaEnabledAndMissingProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var authSalt = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var nonce = Convert.ToBase64String(new byte[] { 5, 6, 7, 8 });
        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.InitAsync(It.IsAny<AuthInitRequest>(), ct))
            .ReturnsAsync(new AuthInitResponse { UserId = Guid.NewGuid().ToString(), AuthSalt = authSalt, Nonce = nonce, MfaEnabled = true });

        var useCase = CreateUseCase(
            serverAuth.Object,
            Mock.Of<ICryptoService>(),
            Mock.Of<IVaultCrypto>(),
            Mock.Of<ISessionContext>(),
            Mock.Of<IAuthTokenService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IPasswordStrengthService>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await useCase.ExecuteAsync(new Email("user@example.com"), "Password123!", mfaProvider: null, ct));

        serverAuth.Verify(x => x.VerifyAsync(It.IsAny<AuthVerifyRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WithMfaEnabledAndBlankCode_ThrowsOperationCanceledException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var authSalt = Convert.ToBase64String(new byte[] { 1, 2, 3, 4 });
        var nonce = Convert.ToBase64String(new byte[] { 5, 6, 7, 8 });
        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.InitAsync(It.IsAny<AuthInitRequest>(), ct))
            .ReturnsAsync(new AuthInitResponse { UserId = Guid.NewGuid().ToString(), AuthSalt = authSalt, Nonce = nonce, MfaEnabled = true });

        var useCase = CreateUseCase(
            serverAuth.Object,
            Mock.Of<ICryptoService>(),
            Mock.Of<IVaultCrypto>(),
            Mock.Of<ISessionContext>(),
            Mock.Of<IAuthTokenService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IPasswordStrengthService>());

        // Act + Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await useCase.ExecuteAsync(new Email("user@example.com"), "Password123!", () => Task.FromResult<string?>(" "), ct));

        serverAuth.Verify(x => x.VerifyAsync(It.IsAny<AuthVerifyRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_BuildsExpectedProof_AndPassesMfaCodeToVerify()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var authSaltBytes = new byte[] { 1, 2, 3, 4 };
        var nonceBytes = new byte[] { 5, 6, 7, 8 };
        var authVerifierBytes = new byte[] { 10, 11, 12, 13, 14, 15 };
        var vaultSaltBytes = new byte[] { 9, 9, 9, 9 };
        var encryptedVault = TestFixtures.CreateEncryptedVault();
        var decryptedVault = new VaultDecrypted { Categories = [], Entries = [] };
        using var masterKey = new MasterKey(new byte[32]);

        using var hmac = new HMACSHA256(authVerifierBytes);
        var expectedProof = Convert.ToBase64String(hmac.ComputeHash(nonceBytes));

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", authSaltBytes)).Returns(authVerifierBytes);
        crypto.Setup(x => x.DeriveMasterKey("Password123!", vaultSaltBytes)).Returns(masterKey);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Decrypt(encryptedVault, masterKey)).Returns(decryptedVault);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var passwordStrength = new Mock<IPasswordStrengthService>();
        passwordStrength.Setup(x => x.Evaluate("Password123!")).Returns(new PasswordStrength { Score = 3 });

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.InitAsync(It.IsAny<AuthInitRequest>(), ct)).ReturnsAsync(new AuthInitResponse
        {
            UserId = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            Nonce = Convert.ToBase64String(nonceBytes),
            MfaEnabled = true
        });
        serverAuth.Setup(x => x.VerifyAsync(It.IsAny<AuthVerifyRequest>(), ct)).ReturnsAsync(new AuthVerifyResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        });
        serverAuth.Setup(x => x.GetUserAsync(userId, ct)).ReturnsAsync(new UserResponse
        {
            Id = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            AuthVerifier = Convert.ToBase64String(authVerifierBytes),
            Email = "user@example.com",
            MfaEnabled = true,
            MfaSecret = Convert.ToBase64String(new byte[] { 1, 1, 1 }),
            ShamirEnabled = false,
            VaultSalt = Convert.ToBase64String(vaultSaltBytes),
            Vault = encryptedVault,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var useCase = CreateUseCase(
            serverAuth.Object,
            crypto.Object,
            vaultCrypto.Object,
            Mock.Of<ISessionContext>(),
            Mock.Of<IAuthTokenService>(),
            users.Object,
            passwordStrength.Object);

        // Act
        await useCase.ExecuteAsync(new Email("user@example.com"), "Password123!", () => Task.FromResult<string?>("654321"), ct);

        // Assert
        serverAuth.Verify(x => x.VerifyAsync(
            It.Is<AuthVerifyRequest>(r =>
                r.UserId == userId &&
                r.MfaCode == "654321" &&
                r.Proof == expectedProof),
            ct), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NewLocalUser_SavesUserSetsTokensAndStartsSession()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var authSaltBytes = new byte[] { 1, 2, 3, 4 };
        var nonceBytes = new byte[] { 9, 9, 9, 9 };
        var vaultSaltBytes = new byte[] { 6, 7, 8, 9 };
        var authVerifierBytes = new byte[] { 10, 11, 12 };
        var encryptedVault = TestFixtures.CreateEncryptedVault();
        var decryptedVault = new VaultDecrypted { Categories = [], Entries = [] };
        using var masterKey = new MasterKey(new byte[32]);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", It.IsAny<byte[]>())).Returns(authVerifierBytes);
        crypto.Setup(x => x.DeriveMasterKey("Password123!", vaultSaltBytes)).Returns(masterKey);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Decrypt(encryptedVault, masterKey)).Returns(decryptedVault);

        var authToken = new Mock<IAuthTokenService>();
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        var passwordStrength = new Mock<IPasswordStrengthService>();
        passwordStrength.Setup(x => x.Evaluate("Password123!")).Returns(new PasswordStrength { Score = 4 });

        var session = new Mock<ISessionContext>();
        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.InitAsync(It.IsAny<AuthInitRequest>(), ct)).ReturnsAsync(new AuthInitResponse
        {
            UserId = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            Nonce = Convert.ToBase64String(nonceBytes),
            MfaEnabled = false
        });
        serverAuth.Setup(x => x.VerifyAsync(It.IsAny<AuthVerifyRequest>(), ct)).ReturnsAsync(new AuthVerifyResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        });
        serverAuth.Setup(x => x.GetUserAsync(userId, ct)).ReturnsAsync(new UserResponse
        {
            Id = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            AuthVerifier = Convert.ToBase64String(authVerifierBytes),
            Email = "user@example.com",
            MfaEnabled = false,
            MfaSecret = null,
            ShamirEnabled = false,
            VaultSalt = Convert.ToBase64String(vaultSaltBytes),
            Vault = encryptedVault,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var useCase = CreateUseCase(serverAuth.Object, crypto.Object, vaultCrypto.Object, session.Object, authToken.Object, users.Object, passwordStrength.Object);

        // Act
        await useCase.ExecuteAsync(new Email("user@example.com"), "Password123!", ct: ct);

        // Assert
        authToken.Verify(x => x.SetToken("access"), Times.Once);
        authToken.Verify(x => x.SetRefreshToken("refresh"), Times.Once);
        users.Verify(x => x.SaveAsync(It.Is<User>(u =>
            u.Id == userId &&
            u.Email.Value == "user@example.com" &&
            ((IEnumerable<byte>)u.AuthSalt).SequenceEqual(authSaltBytes) &&
            ((IEnumerable<byte>)u.AuthVerifier).SequenceEqual(authVerifierBytes))), Times.Once);
        session.Verify(x => x.Start(userId, masterKey, decryptedVault, 4), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ExistingLocalUser_UpdatesExistingEntity()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var existing = CreateUser(userId, "old@example.com");
        var authSaltBytes = new byte[] { 21, 22, 23, 24 };
        var nonceBytes = new byte[] { 1, 1, 1, 1 };
        var vaultSaltBytes = new byte[] { 31, 32, 33, 34 };
        var authVerifierBytes = new byte[] { 41, 42, 43 };
        var encryptedVault = TestFixtures.CreateEncryptedVault();
        var decryptedVault = new VaultDecrypted { Categories = [], Entries = [] };
        using var masterKey = new MasterKey(new byte[32]);

        var crypto = new Mock<ICryptoService>();
        crypto.Setup(x => x.GenerateAuthVerifier("Password123!", It.IsAny<byte[]>())).Returns(authVerifierBytes);
        crypto.Setup(x => x.DeriveMasterKey("Password123!", vaultSaltBytes)).Returns(masterKey);

        var vaultCrypto = new Mock<IVaultCrypto>();
        vaultCrypto.Setup(x => x.Decrypt(encryptedVault, masterKey)).Returns(decryptedVault);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(existing);

        var passwordStrength = new Mock<IPasswordStrengthService>();
        passwordStrength.Setup(x => x.Evaluate("Password123!")).Returns(new PasswordStrength { Score = 2 });

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.InitAsync(It.IsAny<AuthInitRequest>(), ct)).ReturnsAsync(new AuthInitResponse
        {
            UserId = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            Nonce = Convert.ToBase64String(nonceBytes),
            MfaEnabled = false
        });
        serverAuth.Setup(x => x.VerifyAsync(It.IsAny<AuthVerifyRequest>(), ct)).ReturnsAsync(new AuthVerifyResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        });
        serverAuth.Setup(x => x.GetUserAsync(userId, ct)).ReturnsAsync(new UserResponse
        {
            Id = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            AuthVerifier = Convert.ToBase64String(authVerifierBytes),
            Email = "new@example.com",
            MfaEnabled = true,
            MfaSecret = Convert.ToBase64String(new byte[] { 3, 3, 3 }),
            ShamirEnabled = true,
            VaultSalt = Convert.ToBase64String(vaultSaltBytes),
            Vault = encryptedVault,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var useCase = CreateUseCase(
            serverAuth.Object,
            crypto.Object,
            vaultCrypto.Object,
            Mock.Of<ISessionContext>(),
            Mock.Of<IAuthTokenService>(),
            users.Object,
            passwordStrength.Object);

        // Act
        await useCase.ExecuteAsync(new Email("new@example.com"), "Password123!", ct: ct);

        // Assert
        users.Verify(x => x.SaveAsync(It.Is<User>(u => ReferenceEquals(u, existing) && u.Email.Value == "new@example.com" && u.MfaEnabled)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenServerUserVaultMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var authSaltBytes = new byte[] { 1, 2, 3, 4 };
        var nonceBytes = new byte[] { 9, 9, 9, 9 };

        var serverAuth = new Mock<IServerAuthService>();
        serverAuth.Setup(x => x.InitAsync(It.IsAny<AuthInitRequest>(), ct)).ReturnsAsync(new AuthInitResponse
        {
            UserId = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            Nonce = Convert.ToBase64String(nonceBytes),
            MfaEnabled = false
        });
        serverAuth.Setup(x => x.VerifyAsync(It.IsAny<AuthVerifyRequest>(), ct)).ReturnsAsync(new AuthVerifyResponse
        {
            AccessToken = "access",
            RefreshToken = "refresh"
        });
        serverAuth.Setup(x => x.GetUserAsync(userId, ct)).ReturnsAsync(new UserResponse
        {
            Id = userId.ToString(),
            AuthSalt = Convert.ToBase64String(authSaltBytes),
            AuthVerifier = Convert.ToBase64String(new byte[] { 1 }),
            Email = "user@example.com",
            MfaEnabled = false,
            MfaSecret = null,
            ShamirEnabled = false,
            VaultSalt = Convert.ToBase64String(new byte[] { 2 }),
            Vault = null!,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var useCase = CreateUseCase(
            serverAuth.Object,
            Mock.Of<ICryptoService>(x => x.GenerateAuthVerifier(It.IsAny<string>(), It.IsAny<byte[]>()) == new byte[] { 1 }),
            Mock.Of<IVaultCrypto>(),
            Mock.Of<ISessionContext>(),
            Mock.Of<IAuthTokenService>(),
            Mock.Of<IUserRepository>(),
            Mock.Of<IPasswordStrengthService>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await useCase.ExecuteAsync(new Email("user@example.com"), "Password123!", ct: ct));
    }

    private static LoginOnlineUseCase CreateUseCase(
        IServerAuthService serverAuth,
        ICryptoService crypto,
        IVaultCrypto vaultCrypto,
        ISessionContext session,
        IAuthTokenService authToken,
        IUserRepository users,
        IPasswordStrengthService passwordStrength)
        => new(crypto, vaultCrypto, session, serverAuth, authToken, users, passwordStrength);

    private static User CreateUser(Guid id, string email) => new()
    {
        Id = id,
        Email = new Email(email),
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
