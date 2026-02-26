using GoatVaultApplication.Vault;
using GoatVaultCore.Abstractions;
using Moq;

namespace GoatVaultTests.Application.Vault;

public class SyncVaultUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CallsSyncOnce()
    {
        // Arrange
        var syncing = new Mock<ISyncingService>();
        syncing.Setup(x => x.Sync()).Returns(Task.CompletedTask);
        var syncVaultUseCase = new SyncVaultUseCase(syncing.Object);

        // Act
        await syncVaultUseCase.ExecuteAsync();

        // Assert
        syncing.Verify(x => x.Sync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSyncThrows_PropagatesException()
    {
        // Arrange
        var syncing = new Mock<ISyncingService>();
        syncing.Setup(x => x.Sync()).ThrowsAsync(new InvalidOperationException("sync failed"));
        var syncVaultUseCase = new SyncVaultUseCase(syncing.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await syncVaultUseCase.ExecuteAsync());
    }
}
