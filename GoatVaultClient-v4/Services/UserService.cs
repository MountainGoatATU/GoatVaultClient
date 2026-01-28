using GoatVaultClient_v4.Models.API;
using GoatVaultClient_v4.Models.Vault;
using System.Security.Cryptography;
using System.Text;
using Isopoh.Cryptography.Argon2;

namespace GoatVaultClient_v4.Services;

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
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public AuthRegisterRequest RegisterUser(string email, string masterPassword, VaultModel vault)
    {
        var authSalt = new byte[16];
        Rng.GetBytes(authSalt);

        var authVerifier = HashPassword(masterPassword, authSalt);

        return new AuthRegisterRequest
        {
            AuthSalt = Convert.ToBase64String(authSalt),
            AuthVerifier = authVerifier,
            Email = email,
            Vault = vault,
        };

        // return JsonSerializer.Serialize(userPayload, new JsonSerializerOptions { WriteIndented = true });
    }

    // Used during login to generate the auth verifier to compare against stored value
    public string GenerateAuthVerifier(string masterPassword, string authSaltBase64)
    {
        // Convert the stored Base64 salt to byte[]
        var authSalt = Convert.FromBase64String(authSaltBase64);

        // Hash the password using the salt
        var authVerifier = HashPassword(masterPassword, authSalt);

        return authVerifier;
    }

    private static string HashPassword(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var config = new Argon2Config
        {
            Type = Argon2Type.DataIndependentAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = 10,
            MemoryCost = 32768,
            Lanes = 5,
            Threads = Environment.ProcessorCount,
            Password = passwordBytes,
            Salt = salt,
            HashLength = 32
        };

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();

        return Convert.ToBase64String(hash.Buffer);
    }
}