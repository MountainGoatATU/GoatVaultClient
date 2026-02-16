using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;

namespace GoatVaultInfrastructure.Services;

public enum LoginStatus
{
    Success,
    Error
}

public interface IUserService
{
    AuthRegisterRequest RegisterUser(string email, string password, VaultEncrypted? vault);
}

public class UserService : IUserService
{
    public AuthRegisterRequest RegisterUser(string email, string masterPassword, VaultEncrypted? vault)
    {
        var authSalt = CryptoService.GenerateAuthSalt();
        var authVerifier = CryptoService.HashPassword(masterPassword, authSalt);

        return new AuthRegisterRequest
        {
            AuthSalt = Convert.ToBase64String(authSalt),
            AuthVerifier = authVerifier,
            Email = email,
            Vault = vault,
        };
    }
}