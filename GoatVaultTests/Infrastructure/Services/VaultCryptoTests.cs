using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;
using GoatVaultInfrastructure.Services;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultTests.Infrastructure.Services;

public class VaultCryptoTests
{
    private readonly VaultCrypto _vaultCrypto = new();

    [Fact]
    public void EncryptThenDecrypt_RoundTrip_ReturnsOriginalVault()
    {
        // Arrange
        var vault = CreateVault();
        using var key = new MasterKey(CreateFixedKey(1));

        // Act
        var encrypted = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);
        var decrypted = _vaultCrypto.Decrypt(encrypted, key);

        // Assert
        Assert.Single(decrypted.Categories);
        Assert.Single(decrypted.Entries);
        Assert.Equal("Personal", decrypted.Categories[0].Name);
        Assert.Equal("example.com", decrypted.Entries[0].Site);
        Assert.Equal("user@example.com", decrypted.Entries[0].UserName);
        Assert.Equal("P@ssw0rd!", decrypted.Entries[0].Password);
    }

    [Fact]
    public void Encrypt_SameInputTwice_ProducesDifferentNonceAndCiphertext()
    {
        // Arrange
        var vault = CreateVault();
        using var key = new MasterKey(CreateFixedKey(2));

        // Act
        var encrypted1 = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);
        var encrypted2 = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);

        // Assert
        Assert.NotEqual(encrypted1.Nonce, encrypted2.Nonce);
        Assert.NotEqual(encrypted1.EncryptedBlob, encrypted2.EncryptedBlob);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsCryptographicException()
    {
        // Arrange
        var vault = CreateVault();
        using var correctKey = new MasterKey(CreateFixedKey(3));
        using var wrongKey = new MasterKey(CreateFixedKey(4));
        var encrypted = _vaultCrypto.Encrypt(vault, correctKey, vaultSalt: [1, 2, 3]);

        // Act + Assert
        Assert.ThrowsAny<CryptographicException>(() => _vaultCrypto.Decrypt(encrypted, wrongKey));
    }

    [Fact]
    public void Decrypt_WhenCiphertextIsTampered_ThrowsCryptographicException()
    {
        // Arrange
        var vault = CreateVault();
        using var key = new MasterKey(CreateFixedKey(5));
        var encrypted = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);
        encrypted.EncryptedBlob[0] ^= 0x01;

        // Act + Assert
        Assert.ThrowsAny<CryptographicException>(() => _vaultCrypto.Decrypt(encrypted, key));
    }

    [Fact]
    public void Decrypt_WhenAuthTagIsTampered_ThrowsCryptographicException()
    {
        // Arrange
        var vault = CreateVault();
        using var key = new MasterKey(CreateFixedKey(6));
        var encrypted = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);
        encrypted.AuthTag[0] ^= 0x01;

        // Act + Assert
        Assert.ThrowsAny<CryptographicException>(() => _vaultCrypto.Decrypt(encrypted, key));
    }

    [Fact]
    public void Decrypt_WhenNonceIsTampered_ThrowsCryptographicException()
    {
        // Arrange
        var vault = CreateVault();
        using var key = new MasterKey(CreateFixedKey(7));
        var encrypted = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);
        encrypted.Nonce[0] ^= 0x01;

        // Act + Assert
        Assert.ThrowsAny<CryptographicException>(() => _vaultCrypto.Decrypt(encrypted, key));
    }

    [Fact]
    public void EncryptThenDecrypt_RandomizedRoundTrips_PreserveData()
    {
        // Arrange
        var random = new Random(12345);
        using var key = new MasterKey(CreateFixedKey(8));

        // Act + Assert
        for (var i = 0; i < 20; i++)
        {
            var original = CreateRandomVault(random, i);
            var encrypted = _vaultCrypto.Encrypt(original, key, vaultSalt: [1, 2, 3]);
            var decrypted = _vaultCrypto.Decrypt(encrypted, key);

            Assert.Equal(original.Categories.Count, decrypted.Categories.Count);
            Assert.Equal(original.Entries.Count, decrypted.Entries.Count);
            Assert.Equal(original.Entries[0].Site, decrypted.Entries[0].Site);
            Assert.Equal(original.Entries[0].UserName, decrypted.Entries[0].UserName);
            Assert.Equal(original.Entries[0].Password, decrypted.Entries[0].Password);
        }
    }

    [Fact]
    public void Decrypt_WhenCiphertextLengthIsManipulated_ThrowsCryptographicException()
    {
        // Arrange
        var vault = CreateVault();
        using var key = new MasterKey(CreateFixedKey(9));
        var encrypted = _vaultCrypto.Encrypt(vault, key, vaultSalt: [1, 2, 3]);
        var truncatedBlob = encrypted.EncryptedBlob.Take(encrypted.EncryptedBlob.Length - 1).ToArray();
        var malformed = new VaultEncrypted(truncatedBlob, encrypted.Nonce, encrypted.AuthTag)
        {
            EncryptedBlob = truncatedBlob,
            Nonce = encrypted.Nonce,
            AuthTag = encrypted.AuthTag
        };

        // Act + Assert
        Assert.ThrowsAny<CryptographicException>(() => _vaultCrypto.Decrypt(malformed, key));
    }

    [Fact]
    public void Decrypt_WhenPlaintextIsNotValidJson_ThrowsException()
    {
        // Arrange
        using var key = new MasterKey(CreateFixedKey(10));
        var nonce = CryptoService.GenerateNonce();
        var plaintext = "not json"u8.ToArray();
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using (var aesGcm = new AesGcm(key.Key, 16))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var encrypted = new VaultEncrypted(ciphertext, nonce, tag)
        {
            EncryptedBlob = ciphertext,
            Nonce = nonce,
            AuthTag = tag
        };

        // Act + Assert
        Assert.ThrowsAny<Exception>(() => _vaultCrypto.Decrypt(encrypted, key));
    }

    private static VaultDecrypted CreateVault() => new()
    {
        Categories =
        [
            new CategoryItem
            {
                Name = "Personal"
            }
        ],
        Entries =
        [
            new VaultEntry
            {
                Site = "example.com",
                UserName = "user@example.com",
                Password = "P@ssw0rd!",
                Description = "Example account",
                Category = "Personal",
                MfaSecret = "",
                HasMfa = false,
                CurrentTotpCode = null,
                TotpTimeRemaining = 0,
                BreachCount = 0
            }
        ]
    };

    private static byte[] CreateFixedKey(byte seed)
    {
        var key = new byte[32];
        for (var i = 0; i < key.Length; i++)
            key[i] = (byte)(seed + i);
        return key;
    }

    private static VaultDecrypted CreateRandomVault(Random random, int seed)
    {
        var siteSuffix = random.Next(1000, 9999);
        return new VaultDecrypted
        {
            Categories =
            [
                new CategoryItem
                {
                    Name = $"Category-{seed}"
                }
            ],
            Entries =
            [
                new VaultEntry
                {
                    Site = $"site-{siteSuffix}.example",
                    UserName = $"user-{seed}@example.com",
                    Password = $"P@ss-{random.Next(100000, 999999)}",
                    Description = "generated",
                    Category = $"Category-{seed}",
                    MfaSecret = string.Empty,
                    HasMfa = false,
                    CurrentTotpCode = null,
                    TotpTimeRemaining = 0,
                    BreachCount = 0
                }
            ]
        };
    }
}
