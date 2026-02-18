using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Session;

public sealed class SessionContext : ISessionContext
{
    private MasterKey? _masterKey;

    public Guid? UserId { get; private set; }
    public VaultDecrypted? Vault { get; private set; }
    public int MasterPasswordStrength { get; private set; }

    public event EventHandler? VaultChanged;

    public void Start(Guid userId, MasterKey masterKey, VaultDecrypted decryptedVault, int masterPasswordStrength)
    {
        UserId = userId;
        _masterKey = masterKey;
        MasterPasswordStrength = masterPasswordStrength;
        UpdateVault(decryptedVault);
    }

    public void UpdateVault(VaultDecrypted? decryptedVault)
    {
        Vault = decryptedVault;
        RaiseVaultChanged();
    }

    public void RaiseVaultChanged() => VaultChanged?.Invoke(this, EventArgs.Empty);

    public MasterKey GetMasterKey() => _masterKey ?? throw new InvalidOperationException("Session not authenticated.");

    public void End()
    {
        _masterKey?.Dispose();
        _masterKey = null;
        UpdateVault(null);
    }
}
