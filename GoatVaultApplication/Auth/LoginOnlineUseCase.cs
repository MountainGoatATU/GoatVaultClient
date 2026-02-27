using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using System.Security.Cryptography;

namespace GoatVaultApplication.Auth;

public class LoginOnlineUseCase(
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
    ISessionContext session,
    IServerAuthService serverAuth,
    IAuthTokenService authToken,
    IUserRepository users,
    IPasswordStrengthService passwordStrength)
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
        var existing = await users.GetByIdAsync(userId);
        var existingArgon2Parameters = existing?.Argon2Parameters;
        var authArgon2Parameters = existingArgon2Parameters ?? Argon2Parameters.Default;

        // 2. Compute proof client-side
        var authSalt = Convert.FromBase64String(authInitResponse.AuthSalt);
        var authVerifier = await Task.Run(() => crypto.GenerateAuthVerifier(password, authSalt, authArgon2Parameters), ct);

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
            UserId = userId,
            Proof = proof,
            MfaCode = mfaCode
        };
        var authVerifyResponse = await serverAuth.VerifyAsync(authVerifyRequest, ct);
        authToken.SetToken(authVerifyResponse.AccessToken);
        authToken.SetRefreshToken(authVerifyResponse.RefreshToken);

        // 5. Get user
        var userResponse = await serverAuth.GetUserAsync(userId, ct);
        if (userResponse.Vault == null)
            throw new InvalidOperationException("Vault is missing.");

        var argon2Parameters = userResponse.Argon2Parameters ?? existingArgon2Parameters ?? Argon2Parameters.Default;

        // 6. Decrypt vault
        var vaultEncrypted = userResponse.Vault;
        var vaultSalt = Convert.FromBase64String(userResponse.VaultSalt);

        var masterKey = await Task.Run(() => crypto.DeriveMasterKey(password, vaultSalt, argon2Parameters), ct);
        var decryptedVault = await Task.Run(() => vaultCrypto.Decrypt(vaultEncrypted, masterKey), ct);

        // 7. Save to local DB (for offline login & sync baseline)
        User userToSave;

        var mfaSecret = userResponse.MfaSecret is not null
            ? Convert.FromBase64String(userResponse.MfaSecret)
            : [];

        if (existing != null)
        {
            existing.Email = email;
            existing.AuthSalt = authSalt;
            existing.AuthVerifier = authVerifier;
            existing.MfaEnabled = userResponse.MfaEnabled;
            existing.MfaSecret = mfaSecret;
            existing.ShamirEnabled = userResponse.ShamirEnabled;
            existing.VaultSalt = vaultSalt;
            existing.Argon2Parameters = argon2Parameters;
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
                MfaSecret = mfaSecret,
                ShamirEnabled = userResponse.ShamirEnabled,
                VaultSalt = vaultSalt,
                Argon2Parameters = argon2Parameters,
                Vault = vaultEncrypted,
                CreatedAtUtc = userResponse.CreatedAtUtc,
                UpdatedAtUtc = userResponse.UpdatedAtUtc
            };
        }

        await users.SaveAsync(userToSave);

        // 8. Calculate Master Password Strength
        var strength = passwordStrength.Evaluate(password).Score;

        // 9. Start session
        session.Start(userId, masterKey, decryptedVault, strength);
    }
}
