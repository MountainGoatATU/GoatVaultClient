using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Moq;

namespace GoatVaultTests.Application.Vault;

public class CalculateVaultScoreUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSessionMissingData_ThrowsInvalidOperationException()
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns((Guid?)null);
        session.SetupGet(x => x.Vault).Returns((VaultDecrypted?)null);

        var useCase = new CalculateVaultScoreUseCase(session.Object, Mock.Of<IUserRepository>(), Mock.Of<IVaultScoreCalculatorService>());

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await useCase.ExecuteAsync());
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesWithSessionStrengthAndUserMfa()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var vault = new VaultDecrypted
        {
            Categories = [],
            Entries = [new VaultEntry { Password = "p1" }]
        };
        var expected = new VaultScoreDetails
        {
            VaultScore = 900,
            MasterPasswordPercent = 75,
            AveragePasswordsPercent = 80,
            ReuseRatePercent = 100,
            MfaEnabled = true,
            BreachesCount = 0,
            PasswordCount = 1
        };

        var session = new Mock<ISessionContext>();
        session.SetupGet(x => x.UserId).Returns(userId);
        session.SetupGet(x => x.Vault).Returns(vault);
        session.SetupGet(x => x.MasterPasswordStrength).Returns(3);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            Id = userId,
            Email = new GoatVaultCore.Models.Objects.Email("user@example.com"),
            AuthSalt = [1],
            AuthVerifier = [2],
            MfaEnabled = true,
            MfaSecret = [],
            ShamirEnabled = false,
            VaultSalt = [3],
            Vault = new VaultEncrypted([1], [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
            {
                EncryptedBlob = [1],
                Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
                AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
            },
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var calculator = new Mock<IVaultScoreCalculatorService>();
        calculator.Setup(x => x.CalculateScore(vault.Entries, 3, true)).Returns(expected);

        var useCase = new CalculateVaultScoreUseCase(session.Object, users.Object, calculator.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.Equal(expected, result);
    }
}
