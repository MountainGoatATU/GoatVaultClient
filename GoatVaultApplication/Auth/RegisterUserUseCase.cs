using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.Objects;
using GoatVaultCore.Services;
using GoatVaultInfrastructure.Services;

namespace GoatVaultApplication.Auth;

public record PasswordValidationResult(bool IsWarning, bool IsGood, string Message);

public class RegisterUseCase(
    ICryptoService crypto,
    IVaultCrypto vaultCrypto,
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
        var emptyVault = new VaultDecrypted
        {
            Categories = [],
            Entries = []
        };
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

    // NEW: Business logic for password validation moved here
    public async Task<PasswordValidationResult> ValidatePasswordAsync(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return new PasswordValidationResult(false, false, string.Empty);

        // NIST Guidelines: Minimum 8 characters, maximum 64 characters.
        if (password.Length < 8)
            return new PasswordValidationResult(true, false, "NIST Guideline: Password must be at least 8 characters.");

        if (password.Length > 64)
            return new PasswordValidationResult(true, false, "NIST Guideline: Password should not exceed 64 characters.");

        // Dynamic Breach Validation
        try
        {
            var pwnCount = await pwned.CheckPasswordAsync(password);
            if (pwnCount > 0)
                return new PasswordValidationResult(true, false, $"Warning: Password found in {pwnCount} data breaches. It is unsafe.");

            return new PasswordValidationResult(false, true, "Password is secure and hasn't been breached.");
        }
        catch
        {
            // If network fails, don't block the user, just inform them
            return new PasswordValidationResult(true, false, "Unable to reach breach database for verification.");
        }
    }
}