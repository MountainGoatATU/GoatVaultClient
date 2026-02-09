using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Services.Vault;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Linq;

namespace GoatVaultTests;

public class VaultServiceTests
{
    private readonly VaultService _vaultService;

    public VaultServiceTests()
    {
        // Create a mock configuration
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("GOATVAULT_SERVER_BASE_URL").Value)
            .Returns("https://api.example.com/");

        _vaultService = new VaultService(
            configuration: mockConfig.Object,
            goatVaultDb: null,
            httpService: null,
            vaultSessionService: null
        );
    }

    [Fact]
    public void EncryptVault_WithPassword_ReturnsValidPayload()
    {
        // Arrange
        const string password = "StrongPassword123!";
        var vaultData = new VaultData
        {
            Categories = [new CategoryItem { Name = "Work" }, new CategoryItem { Name = "Personal" }],
            Entries =
            [
                new VaultEntry
                {
                    Site = "example.com",
                    UserName = "user@example.com",
                    Password = "password123",
                    Description = "Test entry",
                    Category = "Work"
                }
            ]
        };

        // Act
        var payload = _vaultService.EncryptVault(password, vaultData);

        // Assert
        Assert.NotNull(payload);
        Assert.NotNull(payload.VaultSalt);
        Assert.NotNull(payload.Nonce);
        Assert.NotNull(payload.EncryptedBlob);
        Assert.NotNull(payload.AuthTag);

        // Verify Base64 encoding
        Assert.NotEmpty(Convert.FromBase64String(payload.VaultSalt));
        Assert.NotEmpty(Convert.FromBase64String(payload.Nonce));
        Assert.NotEmpty(Convert.FromBase64String(payload.EncryptedBlob));
        Assert.NotEmpty(Convert.FromBase64String(payload.AuthTag));
    }

    [Fact]
    public void EncryptVault_WithNullVaultData_CreatesDefaultVault()
    {
        // Arrange
        const string password = "StrongPassword123!";

        // Act
        var payload = _vaultService.EncryptVault(password, null);

        // Assert
        Assert.NotNull(payload);
        Assert.NotNull(payload.VaultSalt);
        Assert.NotNull(payload.EncryptedBlob);
    }

    [Fact]
    public void DecryptVault_WithCorrectPassword_ReturnsOriginalData()
    {
        // Arrange
        const string password = "StrongPassword123!";
        var originalData = new VaultData
        {
            Categories = [new CategoryItem { Name = "Work" }, new CategoryItem { Name = "Personal" }, new CategoryItem { Name = "Finance" }],
            Entries =
            [
                new VaultEntry
                {
                    Site = "example.com",
                    UserName = "user@example.com",
                    Password = "password123",
                    Description = "Test entry",
                    Category = "Work"
                }
            ]
        };

        // Act
        var encrypted = _vaultService.EncryptVault(password, originalData);
        var decrypted = _vaultService.DecryptVault(encrypted, password);

        // Assert
        Assert.NotNull(decrypted);
        Assert.Equal(originalData.Categories.Count, decrypted.Categories.Count);
        Assert.Equal(originalData.Categories.Select(c => c.Name), decrypted.Categories.Select(c => c.Name));
        Assert.Equal(originalData.Entries.Count, decrypted.Entries.Count);
        Assert.Equal(originalData.Entries[0].Site, decrypted.Entries[0].Site);
        Assert.Equal(originalData.Entries[0].UserName, decrypted.Entries[0].UserName);
        Assert.Equal(originalData.Entries[0].Password, decrypted.Entries[0].Password);
    }

    [Fact]
    public void DecryptVault_WithWrongPassword_ThrowsException()
    {
        // Arrange
        const string correctPassword = "CorrectPassword123!";
        const string wrongPassword = "WrongPassword123!";
        var vaultData = new VaultData
        {
            Categories = [new CategoryItem { Name = "Work" }],
            Entries = []
        };

        // Act
        var encrypted = _vaultService.EncryptVault(correctPassword, vaultData);

        // Assert
        Assert.Throws<Exception>(() =>
            _vaultService.DecryptVault(encrypted, wrongPassword));
    }

    [Fact]
    public void DecryptVault_WithTamperedData_ThrowsException()
    {
        // Arrange
        const string password = "StrongPassword123!";
        var vaultData = new VaultData { Categories = [], Entries = [] };
        var encrypted = _vaultService.EncryptVault(password, vaultData);

        // Tamper with the encrypted blob
        var tamperedBlob = Convert.FromBase64String(encrypted.EncryptedBlob);
        tamperedBlob[0] ^= 0xFF; // Flip bits
        encrypted.EncryptedBlob = Convert.ToBase64String(tamperedBlob);

        // Assert
        Assert.Throws<Exception>(() =>
            _vaultService.DecryptVault(encrypted, password));
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_PreservesComplexData()
    {
        // Arrange
        const string password = "SuperSecurePassword!@#123";
        var complexData = new VaultData
        {
            Categories = [new CategoryItem { Name = "Work" }, new CategoryItem { Name = "Personal" }, new CategoryItem { Name = "Finance" }, new CategoryItem { Name = "Social Media" }, new CategoryItem { Name = "Entertainment" }],
            Entries =
            [
                new VaultEntry
                {
                    Site = "github.com",
                    UserName = "developer@example.com",
                    Password = "ComplexP@ssw0rd!#$%",
                    Description = "GitHub account with special chars: éñ中文",
                    Category = "Work"
                },

                new VaultEntry
                {
                    Site = "bank.example.com",
                    UserName = "account12345",
                    Password = "SecureBank!2023",
                    Description = "Primary checking account",
                    Category = "Finance"
                }
            ]
        };

        // Act
        var encrypted = _vaultService.EncryptVault(password, complexData);
        var decrypted = _vaultService.DecryptVault(encrypted, password);

        // Assert
        Assert.Equal(complexData.Categories.Select(c => c.Name), decrypted.Categories.Select(c => c.Name));
        Assert.Equal(complexData.Entries.Count, decrypted.Entries.Count);

        for (var i = 0; i < complexData.Entries.Count; i++)
        {
            Assert.Equal(complexData.Entries[i].Site, decrypted.Entries[i].Site);
            Assert.Equal(complexData.Entries[i].UserName, decrypted.Entries[i].UserName);
            Assert.Equal(complexData.Entries[i].Password, decrypted.Entries[i].Password);
            Assert.Equal(complexData.Entries[i].Description, decrypted.Entries[i].Description);
            Assert.Equal(complexData.Entries[i].Category, decrypted.Entries[i].Category);
        }
    }
}