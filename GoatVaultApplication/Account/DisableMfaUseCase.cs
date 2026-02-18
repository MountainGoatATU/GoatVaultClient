using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services;

namespace GoatVaultApplication.Account;

public class DisableMfaUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService crypto,
    IServerAuthService serverAuth)
{
    public async Task ExecuteAsync(string currentPassword)
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

        // 2. Update server
        var updateRequest = new UpdateUserRequest
        {
            MfaEnabled = false,
            // MfaSecret = "" // Send empty string or null?
            // The server logic should handle clearing it if MfaEnabled is false.
            // Or we explicitly clear it.
            MfaSecret = string.Empty // Sending empty string to explicitly clear if server supports it.
            // If server treats empty string as valid secret, that's bad.
            // But typical update logic ignores nulls.
            // We should rely on MfaEnabled=false.
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 3. Update local DB
        user.MfaEnabled = false;
        user.MfaSecret = [];
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);
    }
}