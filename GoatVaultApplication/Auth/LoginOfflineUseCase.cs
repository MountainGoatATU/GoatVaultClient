using GoatVaultCore;

namespace GoatVaultApplication.Auth;

public sealed class LoginOfflineUseCase(
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session)
{
    public async Task ExecuteAsync(Guid localUserId, string password)
    {
        var user = await users.GetByIdAsync(localUserId)
                   ?? throw new UnauthorizedAccessException("Local account not found.");

        var masterKey = crypto.DeriveMasterKey(password, user.VaultSalt);

        if (user.Vault == null)
            throw new InvalidOperationException("Vault is missing.");

        var decryptedVault = vaultCrypto.Decrypt(user.Vault, masterKey);

        session.Start(user.Id, masterKey);
        session.SetVault(decryptedVault);
    }
}
