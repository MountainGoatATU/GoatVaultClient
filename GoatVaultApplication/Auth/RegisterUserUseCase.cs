using GoatVaultCore;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure;

namespace GoatVaultApplication.Auth;

public class RegisterUseCase(
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth)
{
    public async Task ExecuteAsync(Email email, string password)
    {
        // 1. Generate auth salt, auth verifier, & vault salt
        var authSalt = CryptoService.GenerateRandomBytes(32);
        var authVerifier = crypto.GenerateAuthVerifier(password, authSalt);
        var vaultSalt = CryptoService.GenerateRandomBytes(32);

        // 2. Create empty vault and encrypt
        var emptyVault = new VaultDecrypted(); // Empty vault structure
        var masterKey = crypto.DeriveMasterKey(password, vaultSalt); // Vault salt can reuse auth salt or generate separate
        var encryptedVault = vaultCrypto.Encrypt(emptyVault, masterKey);

        // 3. Map to server registration DTO
        var registerPayload = new AuthRegisterRequest()
        {
            Email = email.Value,
            AuthSalt = Convert.ToBase64String(authSalt),
            AuthVerifier = Convert.ToBase64String(authVerifier),
            VaultSalt = Convert.ToBase64String(vaultSalt),
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
        session.Start(localUser.Id, masterKey);
        session.SetVault(emptyVault);
    }
}