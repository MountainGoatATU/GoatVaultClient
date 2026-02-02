using GoatVaultInfrastructure.Services.Vault;

namespace GoatVaultTests;

public class VaultServiceTests
{
    [Fact]
    public void EncryptVault_WithPassword_ReturnsPayload()
    {
        // Arrange
        var vaultService = new VaultService(
            configuration: null,
            goatVaultDb: null,
            httpService: null,
            vaultSessionService: null
        );

        // Act
        var payload = vaultService.EncryptVault(
            masterPassword: "StrongPassword123!",
            vaultData: null
        );

        // Assert
        Assert.NotNull(payload);
    }
}