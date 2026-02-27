using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
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

        var result = await Task.Run(() =>
        {
            // 2. Verify current password
            var currentAuthVerifier = crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt, user.Argon2Parameters);
            if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
            {
                throw new UnauthorizedAccessException("Incorrect current password.");
            }

            // 3. Decrypt vault with current credentials
            var currentMasterKey = crypto.DeriveMasterKey(currentPassword, user.VaultSalt, user.Argon2Parameters);
            var decryptedVault = vaultCrypto.Decrypt(user.Vault, currentMasterKey);

            // 4. Generate new credentials
            var newAuthSalt = CryptoService.GenerateSalt();
            var newArgon2Parameters = Argon2Parameters.Default;
            var newAuthVerifier = crypto.GenerateAuthVerifier(newPassword, newAuthSalt, newArgon2Parameters);
            var newVaultSalt = CryptoService.GenerateSalt();

            // 5. Re-encrypt vault with new credentials
            var newMasterKey = crypto.DeriveMasterKey(newPassword, newVaultSalt, newArgon2Parameters);
            var newEncryptedVault = vaultCrypto.Encrypt(decryptedVault, newMasterKey, newVaultSalt);

            return (decryptedVault, newAuthSalt, newAuthVerifier, newVaultSalt, newEncryptedVault, newMasterKey, newArgon2Parameters);
        });


        // 6. Update server
        var updateRequest = new ChangeMasterPasswordRequest
        {
            AuthSalt = Convert.ToBase64String(result.newAuthSalt),
            AuthVerifier = Convert.ToBase64String(result.newAuthVerifier),
            VaultSalt = Convert.ToBase64String(result.newVaultSalt),
            Argon2Parameters = result.newArgon2Parameters,
            Vault = result.newEncryptedVault
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 7. Update local DB
        user.AuthSalt = result.newAuthSalt;
        user.AuthVerifier = result.newAuthVerifier;
        user.VaultSalt = result.newVaultSalt;
        user.Argon2Parameters = result.newArgon2Parameters;
        user.Vault = result.newEncryptedVault;
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);

        // 8. Calculate new password strength
        var strength = passwordStrength.Evaluate(newPassword).Score;

        // 9. Update session - needs new master key to continue working without re-login
        session.Start(user.Id, result.newMasterKey, result.decryptedVault, strength);
    }
}
