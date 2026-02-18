using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Services;
using Moq;

namespace GoatVaultTests
{
    public class VaultScoreCalculatorServiceTests
    {
        private readonly Mock<IPasswordStrengthService> _strengthMock;
        private readonly IVaultScoreCalculatorService _service;

        public VaultScoreCalculatorServiceTests()
        {
            _strengthMock = new Mock<IPasswordStrengthService>();

            // Default: strong password score = 4 unless specified
            _strengthMock
                .Setup(x => x.Evaluate(It.IsAny<string>()))
                .Returns(new PasswordStrength { Score = 4 });

            _service = new VaultScoreCalculatorService(_strengthMock.Object);
        }

        [Fact]
        public void CalculateScore_EmptyVault_MfaEnabled_MaxScore()
        {
            // Arrange
            var entries = new List<VaultEntry>();
            _strengthMock.Setup(x => x.Evaluate("master"))
                .Returns(new PasswordStrength { Score = 4 });

            // Act
            var result = _service.CalculateScore(entries, "master", true);

            // foundation=400 + uniqueness=200 + behavior=200 + mfa=200 = 1000
            Assert.Equal(1000, result.VaultScore);
            Assert.Equal(0, result.PasswordCount);
            Assert.Equal(100, result.ReuseRatePercent);
            Assert.Equal(100, result.AveragePasswordsPercent);
            Assert.True(result.MfaEnabled);
        }

        [Fact]
        public void CalculateScore_EmptyVault_NoMfa_WeakMasterScore()
        {
            // Arrange
            var entries = new List<VaultEntry>();
            _strengthMock.Setup(x => x.Evaluate("weak"))
                .Returns(new PasswordStrength { Score = 0 });

            // Act
            var result = _service.CalculateScore(entries, "weak", false);

            // uniqueness=200 + behavior=200 = 400 (no MFA, no foundation)
            Assert.Equal(400, result.VaultScore);
            Assert.Equal(0, result.MasterPasswordPercent);
        }

        [Fact]
        public void CalculateScore_MasterPasswordBreached_ZeroesFoundation()
        {
            // Arrange
            var entries = CreateEntries(
                ("site", "pass1", 0),
                ("master-site", "master", 1)
            );

            _strengthMock.Setup(x => x.Evaluate("master"))
                .Returns(new PasswordStrength { Score = 4 });

            _strengthMock.Setup(x => x.Evaluate("pass1"))
                .Returns(new PasswordStrength { Score = 4 });

            // Act
            var result = _service.CalculateScore(entries, "master", true);

            // Foundation should be zeroed
            Assert.Equal(0, result.MasterPasswordPercent);
            Assert.True(result.VaultScore < 1000);
        }

        [Fact]
        public void CalculateScore_WithDuplicatePasswords_ReducesReuseRate()
        {
            // Arrange
            var entries = CreateEntries(
                ("gmail", "same", 0),
                ("github", "same", 0),
                ("bank", "unique", 0)
            );

            _strengthMock.Setup(x => x.Evaluate("master"))
                .Returns(new PasswordStrength { Score = 4 });

            _strengthMock.Setup(x => x.Evaluate("same"))
                .Returns(new PasswordStrength { Score = 4 });

            _strengthMock.Setup(x => x.Evaluate("unique"))
                .Returns(new PasswordStrength { Score = 4 });

            // Act
            var result = _service.CalculateScore(entries, "master", true);

            // 3 passwords, 1 duplicate => reuse < 100%
            Assert.Equal(3, result.PasswordCount);
            Assert.True(result.ReuseRatePercent < 100);
        }

        [Fact]
        public void CalculateScore_WithBreaches_AppliesPenalty()
        {
            // Arrange
            var entries = CreateEntries(
                ("gmail", "p1", 1),
                ("netflix", "p2", 2)
            );

            _strengthMock.Setup(x => x.Evaluate("master"))
                .Returns(new PasswordStrength { Score = 4 });

            _strengthMock.Setup(x => x.Evaluate("p1"))
                .Returns(new PasswordStrength { Score = 4 });

            _strengthMock.Setup(x => x.Evaluate("p2"))
                .Returns(new PasswordStrength { Score = 4 });

            // Act
            var result = _service.CalculateScore(entries, "master", true);

            Assert.Equal(2, result.BreachesCount);
            Assert.True(result.VaultScore < 1000); // penalty applied (2 * 20)
        }

        [Fact]
        public void CalculateScore_NoMfa_CapsScoreAt800()
        {
            // Arrange
            var entries = CreateEntries(
                ("a", "p1", 0),
                ("b", "p2", 0),
                ("c", "p3", 0),
                ("d", "p4", 0)
            );

            _strengthMock.Setup(x => x.Evaluate("master"))
                .Returns(new PasswordStrength { Score = 4 });

            // All strong passwords
            _strengthMock.Setup(x => x.Evaluate(It.IsAny<string>()))
                .Returns(new PasswordStrength { Score = 4 });

            // Act
            var result = _service.CalculateScore(entries, "master", false);

            // Raw would be >800 but should be capped
            Assert.True(result.VaultScore <= 800);
        }

        [Fact]
        public void CalculateScore_AveragePasswordPercent_ComputedCorrectly()
        {
            // Arrange
            var entries = CreateEntries(
                ("site1", "weak", 0),
                ("site2", "strong", 0)
            );

            _strengthMock.Setup(x => x.Evaluate("master"))
                .Returns(new PasswordStrength { Score = 4 });

            _strengthMock.Setup(x => x.Evaluate("weak"))
                .Returns(new PasswordStrength { Score = 0 });

            _strengthMock.Setup(x => x.Evaluate("strong"))
                .Returns(new PasswordStrength { Score = 4 });

            // Act
            var result = _service.CalculateScore(entries, "master", true);

            // avg score = (0+4)/2 = 2 -> 50%
            Assert.Equal(50, result.AveragePasswordsPercent);
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
}
