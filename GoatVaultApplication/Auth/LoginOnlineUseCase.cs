using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;

namespace GoatVaultApplication.Auth;

public class LoginOnlineUseCase(
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth,
    IAuthTokenService authTokenService)
{
    public async Task ExecuteAsync(
        Email email,
        string password,
        Func<Task<string?>>? mfaProvider = null,
        CancellationToken ct = default)
    {
        // 1. Init auth and get salts
        var authInitRequest = new AuthInitRequest { Email = email.Value };
        var authInitResponse = await serverAuth.InitAsync(authInitRequest, ct);
        var userId = Guid.Parse(authInitResponse.UserId);

        // 2. Compute verifier client-side
        var authSalt = Convert.FromBase64String(authInitResponse.AuthSalt);
        var authVerifier = Convert.ToBase64String(crypto.GenerateAuthVerifier(password, authSalt));

        // 3. MFA code if required
        string? mfaCode = null;
        if (authInitResponse.MfaEnabled)
        {
            if (mfaProvider == null)
                throw new InvalidOperationException("MFA is enabled but no provider was supplied.");
            mfaCode = await mfaProvider();
            if (string.IsNullOrWhiteSpace(mfaCode))
                throw new OperationCanceledException("User cancelled MFA input");
        }

        // 4. Verify credentials
        var authVerifyRequest = new AuthVerifyRequest
        {
            UserId = userId, AuthVerifier = authVerifier, MfaCode = mfaCode
        };
        var authVerifyResponse = await serverAuth.VerifyAsync(authVerifyRequest, ct);
        authTokenService.SetToken(authVerifyResponse.AccessToken);
        authTokenService.SetRefreshToken(authVerifyResponse.RefreshToken);

        // 5. Get user
        var user = await serverAuth.GetUserAsync(userId, ct);
        if (user.Vault == null)
            throw new InvalidOperationException("Vault is missing.");

        // 6. Decrypt vault
        var vaultSalt = Convert.FromBase64String(user.VaultSalt);
        var masterKey = crypto.DeriveMasterKey(password, vaultSalt);
        var decryptedVault = vaultCrypto.Decrypt(user.Vault, masterKey);

        // 7. Start session
        session.Start(userId, masterKey, decryptedVault);
    }
}