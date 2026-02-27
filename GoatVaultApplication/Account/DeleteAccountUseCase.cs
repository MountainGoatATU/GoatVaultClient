using GoatVaultApplication.Auth;
using GoatVaultCore.Abstractions;

namespace GoatVaultApplication.Account;

public class DeleteAccountUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService crypto,
    IServerAuthService serverAuth,
    LogoutUseCase logout)
{
    public async Task ExecuteAsync(string currentPassword)
    {
        if (session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        var user = await users.GetByIdAsync(session.UserId.Value)
                   ?? throw new InvalidOperationException("User not found.");

        // Verify current password
        var currentAuthVerifier = await Task.Run(() => crypto.GenerateAuthVerifier(currentPassword, user.AuthSalt, user.Argon2Parameters));
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
        {
            throw new UnauthorizedAccessException("Incorrect current password.");
        }

        // Delete request
        await serverAuth.DeleteUserAsync(user.Id);

        // Logout
        await logout.ExecuteAsync();
    }
}
