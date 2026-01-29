using System.Security.Cryptography;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultCore.Services.Secrets;

namespace GoatVaultInfrastructure.Services;

public enum LoginStatus
{
    Success,
    Error
}

public interface IUserService
{
    AuthRegisterRequest RegisterUser(string email, string password, VaultModel vault);
}

public class UserService : IUserService
{
    public AuthRegisterRequest RegisterUser(string email, string masterPassword, VaultModel vault)
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

        // return JsonSerializer.Serialize(userPayload, new JsonSerializerOptions { WriteIndented = true });
    }
}