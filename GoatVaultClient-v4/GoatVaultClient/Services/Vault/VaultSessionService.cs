using GoatVaultClient.Models.API;
using GoatVaultClient.Models.Vault;

namespace GoatVaultClient.Services.Vault;

public class VaultSessionService
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