using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services;
using GoatVaultInfrastructure.Services;

namespace GoatVaultApplication.Auth;

public class RegisterUseCase(
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth,
    PwnedPasswordService pwned)
{
    public async Task ExecuteAsync(Email email, string password)
    {
        // 0. Check pwned
        var pwnCount = await pwned.CheckPasswordAsync(password);
        if (pwnCount > 0)
        {
            throw new InvalidOperationException("This password has been breached before.");
        }

        // 1. Generate auth salt, auth verifier, & vault salt
        var authSalt = CryptoService.GenerateRandomBytes(32);
        var authVerifier = crypto.GenerateAuthVerifier(password, authSalt);
        var vaultSalt = CryptoService.GenerateRandomBytes(32);

        // 2. Create empty vault and encrypt
        var emptyVault = new VaultDecrypted(); // Empty vault structure
        var masterKey = crypto.DeriveMasterKey(password, vaultSalt);
        var encryptedVault = vaultCrypto.Encrypt(emptyVault, masterKey, vaultSalt);

        // 3. Create registration payload
        var registerPayload = new AuthRegisterRequest()
        {
            Email = email.Value,
            AuthSalt = Convert.ToBase64String(authSalt),
            AuthVerifier = Convert.ToBase64String(authVerifier),
            Vault = encryptedVault
        };

        // 4. Call server
        var response = await serverAuth.RegisterAsync(registerPayload);

        // 5. Map to local user entity
        var localUser = new User
        {
            Id = Guid.Parse(response.Id),
            Email = email,
            AuthSalt = authSalt,
            AuthVerifier = authVerifier,
            VaultSalt = vaultSalt,
            Vault = encryptedVault
        };

        // 6. Save locally
        await users.SaveAsync(localUser);

        // 7. Start session
        session.Start(localUser.Id, masterKey, emptyVault);
    }
}