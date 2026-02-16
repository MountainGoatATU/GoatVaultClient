using GoatVaultCore;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Vault;

namespace GoatVaultApplication.Session;

public sealed class SessionContext : ISessionContext
{
    private MasterKey? _masterKey;

    public Guid? UserId { get; private set; }
    public VaultDecrypted? Vault { get; private set; }

    public void Start(Guid userId, MasterKey masterKey)
    {
        UserId = userId;
        _masterKey = masterKey;
    }

    public void SetVault(VaultDecrypted vault) => Vault = vault;

    public MasterKey GetMasterKey() => _masterKey ?? throw new InvalidOperationException("Session not authenticated.");

    public void End()
    {
        _masterKey?.Dispose();
        _masterKey = null;
        Vault = null;
    }
}
