using GoatVaultCore.Models;
using GoatVaultCore.Models.Vault;

namespace GoatVaultCore;

public interface ISessionContext
{
    Guid? UserId { get; }
    MasterKey GetMasterKey();
    VaultDecrypted? Vault { get; }

    void Start(Guid userId, MasterKey masterKey, VaultDecrypted vaultDecrypted);
    void End();
}
