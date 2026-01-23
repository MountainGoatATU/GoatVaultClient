using GoatVaultClient_v3.Models;
using GoatVaultClient_v3.Services;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace GoatVaultClientTests
{
    [TestFixture]
    public class VaultServiceTests
    {
        private VaultService _vaultService;

        [SetUp]
        public void Setup()
        {
            _vaultService = new VaultService(
                goatVaultDB: null,
                httpService: null,
                vaultSessionService: null
            );
        }

        [Test]
        public void EncryptAndDecryptVault_ValidPassword_ReturnsOriginalData()
        {
            // Arrange
            var password = "StrongPassword123!";
            var originalVault = new VaultData
            {
                Categories = new List<string> { "Email", "Banking" },
                Entries = new List<VaultEntry>()
            };

            // Act
            VaultModel encryptedVault = _vaultService.EncryptVault(password, originalVault);
            VaultData decryptedVault = _vaultService.DecryptVault(encryptedVault, password);

            // Assert
            Assert.That(encryptedVault, Is.Not.Null);
            Assert.That(decryptedVault, Is.Not.Null);
            Assert.That(originalVault.Categories.Count, Is.EqualTo(decryptedVault.Categories.Count));
            CollectionAssert.AreEqual(originalVault.Categories, decryptedVault.Categories);
        }

        [Test]
        public void EncryptVault_NullVaultData_CreatesDefaultVault()
        {
            // Arrange
            var password = "Password123!";
            int expectedCategoryCount = 6; // As per default categories in the service

            // Act
            VaultModel encryptedVault = _vaultService.EncryptVault(password, null);
            VaultData decryptedVault = _vaultService.DecryptVault(encryptedVault, password);

            // Assert
            Assert.That(decryptedVault, Is.Not.Null);
            Assert.That(decryptedVault.Categories, Is.Not.Null);
            Assert.That(decryptedVault.Categories.Count, Is.EqualTo(expectedCategoryCount));
        }

        [Test]
        public void DecryptVault_WrongPassword_ThrowsInvalidOperationException()
        {
            // Arrange
            var correctPassword = "CorrectPassword";
            var wrongPassword = "WrongPassword";

            var vaultData = new VaultData
            {
                Categories = new List<string> { "General" },
                Entries = new List<VaultEntry>()
            };

            VaultModel encryptedVault = _vaultService.EncryptVault(correctPassword, vaultData);

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                _vaultService.DecryptVault(encryptedVault, wrongPassword)
            );

            Assert.That(ex.Message, Does.Contain("Decryption failed"));
        }

        [Test]
        public void DecryptVault_NullPayload_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _vaultService.DecryptVault(null, "password")
            );
        }
    }
}
