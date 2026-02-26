using GoatVaultApplication.Shamir;
using GoatVaultCore.Abstractions;
using Moq;

namespace GoatVaultTests.Application.Shamir;

public class SplitRecoverKeyUseCaseTests
{
    [Fact]
    public async Task SplitKey_MapsSharesToRecoveryShareCollectionWithIndexes()
    {
        // Arrange
        var shamir = new Mock<IShamirSsService>();
        shamir.Setup(x => x.SplitSecret("secret", "pass", 3, 2))
            .Returns(["share one", "share two", "share three"]);

        var useCase = new SplitKeyUseCase(shamir.Object);

        // Act
        var result = await useCase.Execute("secret", "pass", 3, 2);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Index);
        Assert.Equal("share one", result[0].Mnemonic);
        Assert.Equal(3, result[2].Index);
        Assert.Equal("share three", result[2].Mnemonic);
    }

    [Fact]
    public async Task SplitKey_WhenNoShares_ReturnsEmptyCollection()
    {
        // Arrange
        var shamir = new Mock<IShamirSsService>();
        shamir.Setup(x => x.SplitSecret("secret", "pass", 3, 2)).Returns([]);
        var useCase = new SplitKeyUseCase(shamir.Object);

        // Act
        var result = await useCase.Execute("secret", "pass", 3, 2);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task RecoverKey_DelegatesToShamirServiceAndReturnsRecoveredSecret()
    {
        // Arrange
        var shares = new List<string> { "s1", "s2", "s3" };
        var shamir = new Mock<IShamirSsService>();
        shamir.Setup(x => x.RecoverSecret(shares, "pass")).Returns("recovered-secret");
        var useCase = new RecoverKeyUseCase(shamir.Object);

        // Act
        var result = await useCase.Execute(shares, "pass");

        // Assert
        Assert.Equal("recovered-secret", result);
    }

    [Fact]
    public async Task RecoverKey_WhenShamirThrows_PropagatesException()
    {
        // Arrange
        var shamir = new Mock<IShamirSsService>();
        shamir.Setup(x => x.RecoverSecret(It.IsAny<List<string>>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("recovery failed"));
        var useCase = new RecoverKeyUseCase(shamir.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await useCase.Execute(["a", "b", "c"], "pass"));
    }
}
