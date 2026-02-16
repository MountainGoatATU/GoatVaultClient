using GoatVaultCore;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Auth;

public class LoginOnlineUseCase(
    IUserRepository users,
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth)
{
    public async Task<User> ExecuteAsync(
        Email email,
        string password,
        Func<Task<string?>>? mfaProvider = null)
    {
        // Authenticate with server
        var userResponse = await serverAuth.AuthenticateAsync(email, password, mfaProvider);
        if (userResponse == null) throw new UnauthorizedAccessException("Invalid credentials or MFA.");

        // Derive master key from server-provided vault salt
        var masterKey = crypto.DeriveMasterKey(password, Convert.FromBase64String(userResponse.VaultSalt));

        // Decrypt vault
        var encryptedVault = userResponse.Vault ?? throw new InvalidOperationException("Server did not return vault.");
        var decryptedVault = vaultCrypto.Decrypt(encryptedVault, masterKey);

        var localUser = new User
        {
            Id = Guid.Parse(userResponse.Id),
            Email = new Email(userResponse.Email),
            AuthSalt = Convert.FromBase64String(userResponse.AuthSalt),
            AuthVerifier = Convert.FromBase64String(userResponse.AuthVerifier),
            VaultSalt = Convert.FromBase64String(userResponse.VaultSalt),
            Vault = encryptedVault
        };

        await users.SaveAsync(localUser);

        // Start session
        session.Start(localUser.Id, masterKey);
        session.SetVault(decryptedVault);

        return localUser;
    }
}