using GoatVaultCore;
using GoatVaultCore.Models;

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
        var initResp = await serverAuth.InitAsync(email, ct);
        var userId = Guid.Parse(initResp.UserId);

        // 2. Compute verifier client-side
        var verifier = crypto.GenerateAuthVerifier(password, Convert.FromBase64String(initResp.AuthSalt));

        // 3. MFA code if required
        string? mfaCode = null;
        if (initResp.MfaEnabled)
        {
            if (mfaProvider == null)
                throw new InvalidOperationException("MFA is enabled but no provider was supplied.");
            mfaCode = await mfaProvider();
            if (string.IsNullOrWhiteSpace(mfaCode))
                throw new OperationCanceledException("User cancelled MFA input");
        }

        // 4. Verify credentials
        var verifyResp = await serverAuth.VerifyAsync(userId, verifier, mfaCode, ct); 
        authTokenService.SetToken(verifyResp.AccessToken);
        authTokenService.SetRefreshToken(verifyResp.RefreshToken);

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