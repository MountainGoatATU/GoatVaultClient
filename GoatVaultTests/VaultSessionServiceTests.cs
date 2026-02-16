using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services.Vault;

namespace GoatVaultTests;

public class VaultSessionServiceTests
{
    [Fact]
    public void Lock_ClearsAllSessionData()
    {
        // Arrange
        var service = new VaultSessionService
        {
            DecryptedVault = new DecryptedVault { Categories = [], Entries = [] },
            CurrentUser = new UserResponse
            {
                Id = "test-id",
                Email = "test@example.com",
                AuthSalt = "salt",
                Vault = new VaultModel(),
                MfaEnabled = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            MasterPassword = "password123"
        };

        // Act
        service.Lock();

        // Assert
        Assert.Null(service.DecryptedVault);
        Assert.Null(service.CurrentUser);
        Assert.Null(service.MasterPassword);
    }

    [Fact]
    public void SetDecryptedVault_StoresVaultData()
    {
        // Arrange
        var service = new VaultSessionService();
        var vaultData = new DecryptedVault
        {
            Categories = [new CategoryItem { Name = "Work" }, new CategoryItem { Name = "Personal" }],
            Entries = []
        };

        // Act
        service.DecryptedVault = vaultData;

        // Assert
        Assert.NotNull(service.DecryptedVault);
        Assert.Equal(2, service.DecryptedVault.Categories.Count);
    }

    [Fact]
    public void SetCurrentUser_StoresUserData()
    {
        // Arrange
        var service = new VaultSessionService();
        var user = new UserResponse
        {
            Id = "user-123",
            Email = "user@example.com",
            AuthSalt = "salt-value",
            Vault = new VaultModel(),
            MfaEnabled = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        service.CurrentUser = user;

        // Assert
        Assert.NotNull(service.CurrentUser);
        Assert.Equal("user-123", service.CurrentUser.Id);
        Assert.Equal("user@example.com", service.CurrentUser.Email);
    }

    [Fact]
    public void SetMasterPassword_StoresPassword()
    {
        // Arrange
        var service = new VaultSessionService();
        const string password = "SuperSecurePassword123!";

        // Act
        service.MasterPassword = password;

        // Assert
        Assert.Equal(password, service.MasterPassword);
    }
}
