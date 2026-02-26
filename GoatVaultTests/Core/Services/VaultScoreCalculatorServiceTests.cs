using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Services;
using Moq;

namespace GoatVaultTests.Core.Services;

public class VaultScoreCalculatorServiceTests
{
    private readonly Mock<IPasswordStrengthService> _strengthMock;
    private readonly VaultScoreCalculatorService _vaultScoreCalculator;

    public VaultScoreCalculatorServiceTests()
    {
        _strengthMock = new Mock<IPasswordStrengthService>();

        // Default: strong password score = 4 unless specified
        _strengthMock
            .Setup(x => x.Evaluate(It.IsAny<string>()))
            .Returns(new PasswordStrength { Score = 4 });

        _vaultScoreCalculator = new VaultScoreCalculatorService(_strengthMock.Object);
    }

    [Fact]
    public void CalculateScore_WithNullEntriesAndMfaEnabled_ReturnsMaxScore()
    {
        // Act
        var result = _vaultScoreCalculator.CalculateScore(entries: null, masterPasswordScore: 4, mfaEnabled: true);

        // Assert
        Assert.Equal(1000, result.VaultScore);
        Assert.Equal(0, result.PasswordCount);
        Assert.Equal(100, result.MasterPasswordPercent);
        Assert.Equal(100, result.ReuseRatePercent);
        Assert.Equal(100, result.AveragePasswordsPercent);
        Assert.True(result.MfaEnabled);
    }

    [Fact]
    public void CalculateScore_WithEmptyEntriesNoMfaAndWeakMaster_ReturnsExpectedScore()
    {
        // Act
        var result = _vaultScoreCalculator.CalculateScore([], masterPasswordScore: 0, mfaEnabled: false);

        // Assert
        Assert.Equal(400, result.VaultScore);
        Assert.Equal(0, result.MasterPasswordPercent);
        Assert.Equal(0, result.PasswordCount);
    }

    [Fact]
    public void CalculateScore_WithBreachedEntries_AppliesPenaltyAndCountsBreaches()
    {
        // Arrange
        var entries = CreateEntries(
            ("gmail", "p1", 1),
            ("netflix", "p2", 2)
        );

        _strengthMock.Setup(x => x.Evaluate("p1"))
            .Returns(new PasswordStrength { Score = 4 });
        _strengthMock.Setup(x => x.Evaluate("p2"))
            .Returns(new PasswordStrength { Score = 4 });

        // Act
        var result = _vaultScoreCalculator.CalculateScore(entries, masterPasswordScore: 4, mfaEnabled: true);

        // Assert
        Assert.Equal(2, result.BreachesCount);
        Assert.Equal(960, result.VaultScore);
    }

    [Fact]
    public void CalculateScore_ComputesAveragePasswordsPercentFromStrengthService()
    {
        // Arrange
        var entries = CreateEntries(
            ("site1", "weak", 0),
            ("site2", "strong", 0)
        );

        _strengthMock.Setup(x => x.Evaluate("weak"))
            .Returns(new PasswordStrength { Score = 0 });
        _strengthMock.Setup(x => x.Evaluate("strong"))
            .Returns(new PasswordStrength { Score = 4 });

        // Act
        var result = _vaultScoreCalculator.CalculateScore(entries, masterPasswordScore: 4, mfaEnabled: true);

        // Assert
        Assert.Equal(50, result.AveragePasswordsPercent);
    }

    [Fact]
    public void CalculateScore_WhenSameEntryInstanceIsRepeated_ReducesReuseRate()
    {
        // Arrange
        var repeated = new VaultEntry { UserName = "u1", Password = "same", BreachCount = 0 };
        var unique = new VaultEntry { UserName = "u2", Password = "other", BreachCount = 0 };
        var entries = new[] { repeated, repeated, unique };

        _strengthMock.Setup(x => x.Evaluate("same"))
            .Returns(new PasswordStrength { Score = 4 });
        _strengthMock.Setup(x => x.Evaluate("other"))
            .Returns(new PasswordStrength { Score = 4 });

        // Act
        var result = _vaultScoreCalculator.CalculateScore(entries, masterPasswordScore: 4, mfaEnabled: true);

        // Assert
        Assert.Equal(3, result.PasswordCount);
        Assert.Equal(67, result.ReuseRatePercent);
    }

    private static List<VaultEntry> CreateEntries(params (string Name, string Password, int BreachCount)[] items)
    {
        return items.Select(item => new VaultEntry
        {
            UserName = item.Name,
            Password = item.Password,
            BreachCount = item.BreachCount
        }).ToList();
    }
}
