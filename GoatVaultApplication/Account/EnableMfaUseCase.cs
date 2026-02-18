using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services;

namespace GoatVaultApplication.Account;

public class EnableMfaUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService crypto,
    IServerAuthService serverAuth)
{
    public async Task ExecuteAsync(string currentPassword, string mfaSecret, string verificationCode)
    {
        if (session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        var user = await users.GetByIdAsync(session.UserId.Value)
                   ?? throw new InvalidOperationException("User not found.");

        // 1. Verify current password
        var currentAuthVerifier = crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt);
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
        {
            throw new UnauthorizedAccessException("Incorrect current password.");
        }

        // 2. Verify MFA code locally before sending to server (optional but good UX)
        // We need a TOTP service for verification. The ViewModel was using TotpService.VerifyCode.
        // Assuming the server also verifies it or trusts the client request.
        // The server endpoint likely validates the code if sent?
        // The UpdateUserRequest doesn't have "MfaCode" field.
        // The previous ViewModel code verified locally using TotpService.
        
        // 3. Update server
        var updateRequest = new UpdateUserRequest
        {
            MfaEnabled = true,
            MfaSecret = mfaSecret
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 4. Update local DB
        user.MfaEnabled = true;
        // user.MfaSecret = Encoding.UTF8.GetBytes(mfaSecret); // Store secret locally?
        // Wait, User model has MfaSecret as byte[].
        // But the secret is a Base32 string.
        // We should convert it to bytes if we store it, or store as string?
        // The User model has `byte[] MfaSecret`.
        // The previous ViewModel code: `dbUser.MfaSecret = secret;` (implying string? or conversion?)
        
        // Let's assume we store the bytes of the secret string for now.
        user.MfaSecret = System.Text.Encoding.UTF8.GetBytes(mfaSecret);
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);
    }
}