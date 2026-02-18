using System.Security.Cryptography;
using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services;

namespace GoatVaultApplication.Auth;

public class LoginOnlineUseCase(
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth,
    IAuthTokenService authTokenService,
    IUserRepository userRepository)
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

        // 2. Compute proof client-side
        var authSalt = Convert.FromBase64String(authInitResponse.AuthSalt);
        var authVerifier = crypto.GenerateAuthVerifier(password, authSalt);

        var nonce = Convert.FromBase64String(authInitResponse.Nonce);
        using var hmac = new HMACSHA256(authVerifier);
        var proofBytes = hmac.ComputeHash(nonce);
        var proof = Convert.ToBase64String(proofBytes);

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
            UserId = userId, Proof = proof, MfaCode = mfaCode
        };
        var authVerifyResponse = await serverAuth.VerifyAsync(authVerifyRequest, ct);
        authTokenService.SetToken(authVerifyResponse.AccessToken);
        authTokenService.SetRefreshToken(authVerifyResponse.RefreshToken);

        // 5. Get user
        var userResponse = await serverAuth.GetUserAsync(userId, ct);
        if (userResponse.Vault == null)
            throw new InvalidOperationException("Vault is missing.");

        // 6. Decrypt vault
        var vaultEncrypted = userResponse.Vault;
        var vaultSalt = Convert.FromBase64String(userResponse.VaultSalt);

        var masterKey = crypto.DeriveMasterKey(password, vaultSalt);
        var decryptedVault = vaultCrypto.Decrypt(vaultEncrypted, masterKey);

        // 7. Save to local DB (for offline login & sync baseline)
        var existing = await userRepository.GetByIdAsync(userId);
        User userToSave;

        if (existing != null)
        {
            existing.Email = email;
            existing.AuthSalt = authSalt;
            existing.AuthVerifier = authVerifier;
            existing.MfaEnabled = userResponse.MfaEnabled;
            // Preserve existing.MfaSecret if any
            existing.VaultSalt = vaultSalt;
            existing.Vault = vaultEncrypted;
            existing.CreatedAtUtc = userResponse.CreatedAtUtc;
            existing.UpdatedAtUtc = userResponse.UpdatedAtUtc;

            userToSave = existing;
        }
        else
        {
            userToSave = new User
            {
                Id = userId,
                Email = email,
                AuthSalt = authSalt,
                AuthVerifier = authVerifier,
                MfaEnabled = userResponse.MfaEnabled,
                VaultSalt = vaultSalt,
                Vault = vaultEncrypted,
                CreatedAtUtc = userResponse.CreatedAtUtc,
                UpdatedAtUtc = userResponse.UpdatedAtUtc
            };
        }

        await userRepository.SaveAsync(userToSave);

        // 8. Calculate Master Password Strength
        var strength = PasswordStrengthService.Evaluate(password).Score;

        // 9. Start session
        session.Start(userId, masterKey, decryptedVault, strength);
    }
}