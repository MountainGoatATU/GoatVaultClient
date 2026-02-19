using GoatVaultCore.Models;
using GoatVaultCore.Models.Objects;

namespace GoatVaultCore.Abstractions;

public interface ISessionContext
{
    Guid? UserId { get; }
    VaultDecrypted? Vault { get; }
    int MasterPasswordStrength { get; }

    void Start(Guid userId, MasterKey masterKey, VaultDecrypted vaultDecrypted, int masterPasswordStrength);
    void UpdateVault(VaultDecrypted vaultDecrypted);
    void RaiseVaultChanged();
    event EventHandler? VaultChanged;
    MasterKey GetMasterKey();
    void End();
}
