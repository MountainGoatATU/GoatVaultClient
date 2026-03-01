using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.API;

namespace GoatVaultApplication.Shamir;

public class EnableShamirUseCase(
    ISessionContext session,
    IUserRepository users,
    ICryptoService cryptoService,
    IServerAuthService serverAuth)
{
    public async Task ExecuteAsync(string currentPassword)
    {
        if (session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        var user = await users.GetByIdAsync(session.UserId.Value)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.ShamirEnabled)
            throw new InvalidOperationException("Shamir's Secret Sharing is already enabled for this user.");

        // 1. Verify current password
        var currentAuthVerifier = await Task.Run(() => cryptoService.GenerateAuthVerifier(currentPassword, user.AuthSalt, user.Argon2Parameters));
        if (!currentAuthVerifier.SequenceEqual(user.AuthVerifier))
            throw new UnauthorizedAccessException("Incorrect current password.");

        // 2. Update server
        var updateRequest = new UpdateShamirRequest { ShamirEnabled = true };
        var serverUser = await serverAuth.UpdateUserAsync(user.Id, updateRequest);

        // 3. Update local DB
        user.ShamirEnabled = serverUser.ShamirEnabled;
        user.UpdatedAtUtc = serverUser.UpdatedAtUtc;

        await users.SaveAsync(user);
    }
}
