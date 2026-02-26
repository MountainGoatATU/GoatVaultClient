using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Vault;

public class DeleteVaultEntryUseCase(ISessionContext session, SaveVaultUseCase saveVault)
{
    public async Task ExecuteAsync(VaultEntry entry)
    {
        if (session.Vault is null)
        {
            throw new InvalidOperationException("Vault not loaded.");
        }

        session.Vault.Entries.Remove(entry);
        session.RaiseVaultChanged();
        await saveVault.ExecuteAsync();
    }
}