using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;

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
        var currentAuthVerifier = await Task.Run(() => crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt));
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
        {
            throw new UnauthorizedAccessException("Incorrect current password.");
        }

        // 2. Update server
        var updateRequest = new DisableMfaRequest
        {
            MfaEnabled = false,
            MfaSecret = null
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 3. Update local DB
        user.MfaEnabled = false;
        user.MfaSecret = [];
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);
    }
}