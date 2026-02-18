using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using GoatVaultCore.Services;
using GoatVaultInfrastructure.Services;

namespace GoatVaultApplication.Auth;

public class RegisterUseCase(
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth,
    PwnedPasswordService pwned,
    LoginOnlineUseCase loginOnline)
{
    public async Task ExecuteAsync(Email email, string password)
    {
        // 1. Check pwned
        var pwnCount = await pwned.CheckPasswordAsync(password);
        if (pwnCount > 0)
        {
            throw new InvalidOperationException("This password has been breached before.");
        }

        // 2. Generate auth salt, auth verifier, & vault salt
        var authSalt = CryptoService.GenerateRandomBytes(32);
        var authVerifier = crypto.GenerateAuthVerifier(password, authSalt);
        var vaultSalt = CryptoService.GenerateRandomBytes(32);

        // 3. Create empty vault and encrypt
        var emptyVault = new VaultDecrypted(); // Empty vault structure
        var masterKey = crypto.DeriveMasterKey(password, vaultSalt);
        var encryptedVault = vaultCrypto.Encrypt(emptyVault, masterKey, vaultSalt);

        // 4. Create registration payload
        var registerPayload = new AuthRegisterRequest()
        {
            Email = email.Value,
            AuthSalt = Convert.ToBase64String(authSalt),
            AuthVerifier = Convert.ToBase64String(authVerifier),
            VaultSalt = Convert.ToBase64String(vaultSalt),
            Vault = encryptedVault
        };

        // 5. Call server
        await serverAuth.RegisterAsync(registerPayload);

        // 6. Automatically log in to establish session and get auth token
        await loginOnline.ExecuteAsync(email, password);
    }
}