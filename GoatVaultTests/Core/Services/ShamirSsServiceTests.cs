using GoatVaultCore.Services;
using Xecrets.Slip39;

namespace GoatVaultTests.Core.Services;

public class ShamirSsServiceTests
{
    private readonly ShamirSsService _shamir = new(new ShamirsSecretSharing(new StrongRandom()));

    [Fact]
    public void SplitSecret_WithValidInputs_ReturnsRequestedNumberOfShares()
    {
        // Act
        var shares = _shamir.SplitSecret("my-very-secret", "passphrase", totalShares: 5, threshold: 3);

        // Assert
        Assert.Equal(5, shares.Count);
        Assert.All(shares, share => Assert.False(string.IsNullOrWhiteSpace(share)));
    }

    [Fact]
    public void SplitSecret_AndRecoverSecret_WithThresholdShares_ReturnsOriginalSecret()
    {
        // Arrange
        const string secret = "my-very-secret";
        const string passphrase = "passphrase";

        // Act
        var shares = _shamir.SplitSecret(secret, passphrase, totalShares: 5, threshold: 3);
        var recovered = _shamir.RecoverSecret(shares.Take(3).ToList(), passphrase);

        // Assert
        Assert.Equal(secret, recovered);
    }

    [Fact]
    public void RecoverSecret_WithNoShares_ThrowsArgumentException()
    {
        // Act
        var ex = Assert.Throws<ArgumentException>(() => _shamir.RecoverSecret([], "passphrase"));

        // Assert
        Assert.Contains("Shares are required for recovery", ex.Message);
    }

    [Fact]
    public void RecoverSecret_WithInsufficientShares_ThrowsInvalidOperationException()
    {
        // Act
        var shares = _shamir.SplitSecret("my-very-secret", "passphrase", totalShares: 5, threshold: 3);

        // Assert
        Assert.Throws<InvalidOperationException>(() => _shamir.RecoverSecret(shares.Take(2).ToList(), "passphrase"));
    }

    [Fact]
    public void RecoverSecret_WithInvalidShareFormat_ThrowsSlip39Exception()
    {
        // Act
        var shares = _shamir.SplitSecret("my-very-secret", "passphrase", totalShares: 5, threshold: 3);
        shares[0] = "invalid mnemonic share";

        // Assert
        Assert.Throws<Slip39Exception>(() => _shamir.RecoverSecret(shares.Take(3).ToList(), "passphrase"));
    }

    [Fact]
    public void TestShamir_RoundTrip_ReturnsOriginalSecret()
    {
        // Arrange
        const string secret = "integration-secret";

        // Act
        var recovered = _shamir.TestShamir(secret, "passphrase");

        // Assert
        Assert.Equal(secret, recovered);
    }
}
