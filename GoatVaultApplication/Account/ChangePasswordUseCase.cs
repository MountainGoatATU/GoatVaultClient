using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using GoatVaultInfrastructure.Services;

namespace GoatVaultApplication.Account;

public class ChangePasswordUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    IServerAuthService serverAuth,
    IPasswordStrengthService passwordStrength)
{
    public async Task ExecuteAsync(string currentPassword, string newPassword)
    {
        if (session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        // 1. Get user
        var user = await users.GetByIdAsync(session.UserId.Value)
                   ?? throw new InvalidOperationException("User not found.");

        // 2. Verify current password
        var currentAuthVerifier = crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt);
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
        {
            throw new UnauthorizedAccessException("Incorrect current password.");
        }

        // 3. Decrypt vault with current credentials
        // Although we likely have it decrypted in session, decrypting from source ensures integrity before re-encryption
        var currentMasterKey = crypto.DeriveMasterKey(currentPassword, user.VaultSalt);
        var decryptedVault = vaultCrypto.Decrypt(user.Vault, currentMasterKey);

        // 4. Generate new credentials
        var newAuthSalt = CryptoService.GenerateSalt();
        var newAuthVerifier = crypto.GenerateAuthVerifier(newPassword, newAuthSalt);
        var newVaultSalt = CryptoService.GenerateSalt();

        // 5. Re-encrypt vault with new credentials
        var newMasterKey = crypto.DeriveMasterKey(newPassword, newVaultSalt);
        var newEncryptedVault = vaultCrypto.Encrypt(decryptedVault, newMasterKey, newVaultSalt);

        // 6. Update server
        var updateRequest = new ChangeMasterPasswordRequest
        {
            AuthSalt = Convert.ToBase64String(newAuthSalt),
            AuthVerifier = Convert.ToBase64String(newAuthVerifier),
            VaultSalt = Convert.ToBase64String(newVaultSalt),
            Vault = newEncryptedVault
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 7. Update local DB
        user.AuthSalt = newAuthSalt;
        user.AuthVerifier = newAuthVerifier;
        user.VaultSalt = newVaultSalt;
        user.Vault = newEncryptedVault;
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);

        // 8. Calculate new password strength
        var strength = passwordStrength.Evaluate(newPassword).Score;

        // 9. Update session - needs new master key to continue working without re-login
        session.Start(user.Id, newMasterKey, decryptedVault, strength);
    }
}