using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Vault;

public class LoadVaultUseCase(
    IUserRepository users,
    ISessionContext session,
    IVaultCrypto crypto)
{
    public async Task<VaultDecrypted> ExecuteAsync()
    {
        var masterKey = session.GetMasterKey();

        var userId = session.UserId
                     ?? throw new InvalidOperationException("User not logged in.");

        var user = await users.GetByIdAsync(userId)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.Vault == null)
            throw new InvalidOperationException("Vault missing.");

        var decryptedVault = crypto.Decrypt(user.Vault, masterKey);

        session.UpdateVault(decryptedVault);

        return decryptedVault;
    }
}