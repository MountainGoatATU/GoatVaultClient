using GoatVaultCore.Models;
using GoatVaultCore.Models.API;

namespace GoatVaultCore;

public interface IServerAuthService
{
    Task<UserResponse> AuthenticateAsync(Email email, string password, Func<Task<string?>>? mfaProvider);
    Task<AuthRegisterResponse> RegisterAsync(AuthRegisterRequest authRegisterRequest);
}