using GoatVaultApplication.Session;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;

namespace GoatVaultTests.Application.Session;

public class SessionContextTests
{
    [Fact]
    public void GetMasterKey_WhenSessionNotStarted_ThrowsInvalidOperationException()
    {
        // Arrange
        var ctx = new SessionContext();

        // Act
        var ex = Assert.Throws<InvalidOperationException>(ctx.GetMasterKey);

        // Assert
        Assert.Equal("Session not authenticated.", ex.Message);
    }

    [Fact]
    public void Start_SetsSessionState_AndRaisesVaultChangedOnce()
    {
        // Arrange
        var ctx = new SessionContext();
        var userId = Guid.NewGuid();
        var key = new MasterKey([1, 2, 3, 4]);
        var vault = CreateVault();
        var raisedCount = 0;
        ctx.VaultChanged += (_, _) => raisedCount++;

        // Act
        ctx.Start(userId, key, vault, masterPasswordStrength: 4);

        // Assert
        Assert.Equal(userId, ctx.UserId);
        Assert.Same(vault, ctx.Vault);
        Assert.Equal(4, ctx.MasterPasswordStrength);
        Assert.Same(key, ctx.GetMasterKey());
        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public void UpdateVault_SetsVault_AndRaisesVaultChanged()
    {
        // Arrange
        var ctx = new SessionContext();
        var vault = CreateVault();
        var raisedCount = 0;
        ctx.VaultChanged += (_, _) => raisedCount++;

        // Act
        ctx.UpdateVault(vault);

        // Assert
        Assert.Same(vault, ctx.Vault);
        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public void RaiseVaultChanged_RaisesVaultChangedEvent()
    {
        // Arrange
        var ctx = new SessionContext();
        var raisedCount = 0;
        ctx.VaultChanged += (_, _) => raisedCount++;

        // Act
        ctx.RaiseVaultChanged();

        // Assert
        Assert.Equal(1, raisedCount);
    }

    [Fact]
    public void End_DisposesMasterKey_ClearsVault_AndRaisesVaultChanged()
    {
        // Arrange
        var ctx = new SessionContext();
        var masterKeyBytes = new byte[] { 9, 8, 7, 6 };
        var masterKey = new MasterKey(masterKeyBytes);
        var raisedCount = 0;
        ctx.VaultChanged += (_, _) => raisedCount++;
        ctx.Start(Guid.NewGuid(), masterKey, CreateVault(), masterPasswordStrength: 3);

        // Act
        ctx.End();

        // Assert
        Assert.Null(ctx.Vault);
        Assert.Equal("\0\0\0\0"u8.ToArray(), masterKeyBytes);
        Assert.Throws<InvalidOperationException>(ctx.GetMasterKey);
        Assert.Equal(2, raisedCount);
    }

    private static VaultDecrypted CreateVault() => new()
    {
        Categories = [],
        Entries = []
    };
}
