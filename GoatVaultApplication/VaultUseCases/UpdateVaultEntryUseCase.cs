using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.VaultUseCases;

public class UpdateVaultEntryUseCase(ISessionContext session, SaveVaultUseCase saveVault)
{
    public async Task ExecuteAsync(VaultEntry oldEntry, VaultEntry newEntry)
    {
        if (session.Vault is null)
        {
            throw new InvalidOperationException("Vault not loaded.");
        }

        var index = session.Vault.Entries.IndexOf(oldEntry);
        if (index >= 0)
        {
            session.Vault.Entries[index] = newEntry;
        }
        else
        {
             session.Vault.Entries.Add(newEntry);
        }
        session.RaiseVaultChanged();
        await saveVault.ExecuteAsync();
    }
}