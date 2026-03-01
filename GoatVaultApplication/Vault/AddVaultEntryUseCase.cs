using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Vault;

public class AddVaultEntryUseCase(ISessionContext session, SaveVaultUseCase saveVault)
{
    public async Task ExecuteAsync(VaultEntry entry)
    {
        if (session.Vault is null)
            throw new InvalidOperationException("Vault not loaded.");

        session.Vault.Entries.Add(entry);
        session.RaiseVaultChanged();
        await saveVault.ExecuteAsync();
    }
}