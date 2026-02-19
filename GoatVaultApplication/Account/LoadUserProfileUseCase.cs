using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Account;

public class LoadUserProfileUseCase(
    ISessionContext session,
    IUserRepository users)
{
    public async Task<User> ExecuteAsync()
    {
        if (session.UserId == null)
            throw new InvalidOperationException("No user logged in.");

        var user = await users.GetByIdAsync(session.UserId.Value);
        return user ?? throw new InvalidOperationException("User profile not found.");
    }
}