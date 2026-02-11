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

    public event Action? VaultEntriesChanged;
    public event Action? MasterPasswordChanged;

    public List<VaultEntry> VaultEntries => DecryptedVault?.Entries.ToList() ?? [];

    // CRUD methods for vault entries
    public void AddEntry(VaultEntry entry)
    {
        if (DecryptedVault == null) return;
        DecryptedVault.Entries.Add(entry);
        VaultEntriesChanged?.Invoke();
    }

    public void RemoveEntry(VaultEntry entry)
    {
        if (DecryptedVault == null) return;
        DecryptedVault.Entries.Remove(entry);
        VaultEntriesChanged?.Invoke();
    }

    public void UpdateEntry(VaultEntry oldEntry, VaultEntry newEntry)
    {
        if (DecryptedVault == null) return;
        var index = DecryptedVault.Entries.IndexOf(oldEntry);
        if (index >= 0)
            DecryptedVault.Entries[index] = newEntry;
        VaultEntriesChanged?.Invoke();
    }

    // Master password change method
    public void ChangeMasterPassword(string newPassword)
    {
        MasterPassword = newPassword;
        MasterPasswordChanged?.Invoke();
    }

    public void Lock()
    {
        DecryptedVault = null;
        CurrentUser = null;
        MasterPassword = null;

        // Force Garbage Collection to remove secrets from heap (Optional but good)
        GC.Collect();
    }

    public void RaiseVaultEntriesChanged() => VaultEntriesChanged?.Invoke();
    public void RaiseMasterPasswordChanged() => MasterPasswordChanged?.Invoke();
}