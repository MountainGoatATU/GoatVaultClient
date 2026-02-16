using GoatVaultCore.Models;
using GoatVaultCore.Models.Vault;

namespace GoatVaultCore;

public interface IVaultCrypto
{
    VaultEncrypted Encrypt(VaultDecrypted decryptedVault, MasterKey masterKey);
    VaultDecrypted Decrypt(VaultEncrypted encryptedVault, MasterKey masterKey);
}