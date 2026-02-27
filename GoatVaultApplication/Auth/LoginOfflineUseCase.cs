using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Auth;

public sealed class LoginOfflineUseCase(
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IPasswordStrengthService passwordStrength)
{
    public async Task ExecuteAsync(Guid localUserId, string password)
    {
        // 1. Get user
        var user = await users.GetByIdAsync(localUserId)
                   ?? throw new UnauthorizedAccessException("Local account not found.");

        if (user.Vault == null)
            throw new InvalidOperationException("Vault is missing.");

        // 2. Decrypt vault
        var masterKey = await Task.Run(() => crypto.DeriveMasterKey(password, user.VaultSalt, user.Argon2Parameters));
        var decryptedVault = await Task.Run(() => vaultCrypto.Decrypt(user.Vault, masterKey));

        // 3. Start session
        var strength = passwordStrength.Evaluate(password).Score;
        session.Start(user.Id, masterKey, decryptedVault, strength);
    }
}
