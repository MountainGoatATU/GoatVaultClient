using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using System.Text;

namespace GoatVaultApplication.Account;

public class EnableMfaUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService crypto,
    IServerAuthService serverAuth)
{
    public async Task ExecuteAsync(string currentPassword, string mfaSecret)
    {
        if (session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        var user = await users.GetByIdAsync(session.UserId.Value)
                   ?? throw new InvalidOperationException("User not found.");

        // 1. Verify current password
        var currentAuthVerifier = await Task.Run(() => crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt, user.Argon2Parameters));
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
        {
            throw new UnauthorizedAccessException("Incorrect current password.");
        }

        // 2. Update server
        var updateRequest = new EnableMfaRequest
        {
            MfaEnabled = true,
            MfaSecret = mfaSecret
        };

        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 3. Update local DB
        user.MfaEnabled = true;
        user.MfaSecret = Encoding.UTF8.GetBytes(mfaSecret);
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);
    }
}
