using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.VaultUseCases;

public sealed class SaveVaultUseCase(
    IUserRepository users,
    ISessionContext session,
    IVaultCrypto crypto)
{
    public async Task ExecuteAsync()
    {
        var masterKey = session.GetMasterKey();
        var vault = session.Vault
                    ?? throw new InvalidOperationException("Vault not loaded.");

        var encrypted = crypto.Encrypt(vault, masterKey);

        var user = await users.GetByIdAsync(session.UserId!.Value)
                   ?? throw new InvalidOperationException("User not found.");

        user.Vault = encrypted;

        await users.SaveAsync(user);
    }
}
