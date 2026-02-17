using GoatVaultCore.Models;

namespace GoatVaultCore.Abstractions;

public interface ISessionContext
{
    Guid? UserId { get; }
    VaultDecrypted? Vault { get; }

    void Start(Guid userId, MasterKey masterKey, VaultDecrypted vaultDecrypted);
    void UpdateVault(VaultDecrypted vaultDecrypted);
    void RaiseVaultChanged();
    event EventHandler? VaultChanged;
    MasterKey GetMasterKey();
    void End();
}
