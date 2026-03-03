using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Vault;

public sealed class WipeVaultUseCase(
    ISessionContext session, 
    SaveVaultUseCase saveVault)
{
    public async Task ExecuteAsync()
    {
        if (session.Vault is null)
            throw new InvalidOperationException("Vault not loaded.");

        session.Vault.Categories.Clear();
        session.Vault.Entries.Clear();
        session.RaiseVaultChanged();
        await saveVault.ExecuteAsync();
    }
}
