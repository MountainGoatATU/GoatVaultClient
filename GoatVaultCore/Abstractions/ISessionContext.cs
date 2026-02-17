using GoatVaultCore.Models;

namespace GoatVaultCore.Abstractions;

public interface ISessionContext
{
    Guid? UserId { get; }
    VaultDecrypted? Vault { get; }

    void Start(Guid userId, MasterKey masterKey, VaultDecrypted vaultDecrypted);
    void UpdateVault(VaultDecrypted vaultDecrypted);
    MasterKey GetMasterKey();
    void End();
}
