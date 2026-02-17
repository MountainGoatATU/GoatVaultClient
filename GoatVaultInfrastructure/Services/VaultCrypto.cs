using System.Security.Cryptography;
using System.Text;
using GoatVaultCore.Models;
using GoatVaultCore.Abstractions;

namespace GoatVaultInfrastructure.Services;

public class VaultCrypto : IVaultCrypto
{
    public VaultDecrypted Decrypt(VaultEncrypted encryptedVault, MasterKey key)
    {
        var plaintext = new byte[encryptedVault.EncryptedBlob.Length];

        using var aesGcm = new AesGcm(key.Key, 16);
        aesGcm.Decrypt(encryptedVault.Nonce, encryptedVault.EncryptedBlob, encryptedVault.AuthTag, plaintext);

        var json = Encoding.UTF8.GetString(plaintext);
        return System.Text.Json.JsonSerializer.Deserialize<VaultDecrypted>(json)
               ?? throw new InvalidOperationException("Vault decryption returned null");
    }

    public VaultEncrypted Encrypt(VaultDecrypted decryptedVault, MasterKey key)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(decryptedVault);
        var plaintext = Encoding.UTF8.GetBytes(json);

        var nonce = CryptoService.GenerateRandomBytes(12);
        var authTag = new byte[16];
        var encryptedBlob = new byte[plaintext.Length];

        using var aesGcm = new AesGcm(key.Key, 16);
        aesGcm.Encrypt(nonce, plaintext, encryptedBlob, authTag);

        return new VaultEncrypted(encryptedBlob, nonce, authTag);
    }
}