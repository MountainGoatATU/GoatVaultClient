using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Services;

namespace GoatVaultApplication.Account;

public class ChangeEmailUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService crypto,
    IServerAuthService serverAuth)
{
    public async Task ExecuteAsync(string currentPassword, Email newEmail)
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
            Email = newEmail.Value
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 3. Update local DB
        user.Email = newEmail;
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);
    }
}