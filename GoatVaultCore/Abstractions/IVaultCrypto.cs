using GoatVaultCore.Models;

namespace GoatVaultCore.Abstractions;

public interface IVaultCrypto
{
    VaultEncrypted Encrypt(VaultDecrypted decryptedVault, MasterKey masterKey);
    VaultDecrypted Decrypt(VaultEncrypted encryptedVault, MasterKey masterKey);
}