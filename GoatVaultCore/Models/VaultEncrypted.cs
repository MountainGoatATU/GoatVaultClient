using System.Text.Json.Serialization;
using GoatVaultCore;

namespace GoatVaultCore.Models;

public class VaultEncrypted
{
    [JsonConverter(typeof(Base64Converter))]
    public byte[] VaultSalt { get; set; }

    [JsonConverter(typeof(Base64Converter))]
    public byte[] EncryptedBlob { get; set; }

    [JsonConverter(typeof(Base64Converter))]
    public byte[] Nonce { get; set; }

    [JsonConverter(typeof(Base64Converter))]
    public byte[] AuthTag { get; set; }

    public VaultEncrypted(byte[] vaultSalt, byte[] encryptedBlob, byte[] nonce, byte[] authTag)
    {
        VaultSalt = vaultSalt ?? throw new ArgumentNullException(nameof(vaultSalt));
        EncryptedBlob = encryptedBlob ?? throw new ArgumentNullException(nameof(encryptedBlob));
        Nonce = nonce ?? throw new ArgumentNullException(nameof(nonce));
        AuthTag = authTag ?? throw new ArgumentNullException(nameof(authTag));

        if (Nonce.Length != 12) // AES-GCM standard nonce
            throw new ArgumentException("Nonce must be 12 bytes.", nameof(nonce));

        if (AuthTag.Length != 16) // AES-GCM standard tag
            throw new ArgumentException("Tag must be 16 bytes.", nameof(authTag));
    }
}