using GoatVaultApplication.Auth;
using GoatVaultCore.Abstractions;
using Moq;

namespace GoatVaultTests.Application.Auth;

public class LogoutUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_StopsPeriodicSync_ThenEndsSession()
    {
        // Arrange
        var sequence = new MockSequence();
        var session = new Mock<ISessionContext>();
        var syncing = new Mock<ISyncingService>();

        syncing.InSequence(sequence).Setup(x => x.StopPeriodicSync());
        session.InSequence(sequence).Setup(x => x.End());

        var logoutUseCase = new LogoutUseCase(session.Object, syncing.Object);

        // Act
        await logoutUseCase.ExecuteAsync();

        // Assert
        syncing.Verify(x => x.StopPeriodicSync(), Times.Once);
        session.Verify(x => x.End(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenStopPeriodicSyncThrows_DoesNotEndSession()
    {
        // Arrange
        var session = new Mock<ISessionContext>();
        var syncing = new Mock<ISyncingService>();
        syncing.Setup(x => x.StopPeriodicSync()).Throws(new InvalidOperationException("sync stop failed"));

        var logoutUseCase = new LogoutUseCase(session.Object, syncing.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await logoutUseCase.ExecuteAsync());
        session.Verify(x => x.End(), Times.Never);
    }
}
