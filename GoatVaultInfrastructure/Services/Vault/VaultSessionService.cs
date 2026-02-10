using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;

namespace GoatVaultInfrastructure.Services.Vault;

public interface IVaultSessionService
{
    void Lock();
}

public class VaultSessionService : IVaultSessionService
{
    public VaultData? DecryptedVault { get; set; } = null;
    public UserResponse? CurrentUser { get; set; } = null;
    public string? MasterPassword { get; set; } = "";

    public void Lock()
    {
        DecryptedVault = null;
        CurrentUser = null;
        MasterPassword = null;

        // Force Garbage Collection to remove secrets from heap (Optional but good)
        GC.Collect();
    }
}