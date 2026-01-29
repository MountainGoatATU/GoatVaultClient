using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultCore.Services.Secrets;

public static class CryptoService
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public static string GenerateAuthVerifier(string masterPassword, string authSaltBase64)
    {
        var authSalt = Convert.FromBase64String(authSaltBase64);
        var authVerifier = HashPassword(masterPassword, authSalt);

        return authVerifier;
    }

    public static byte[] GenerateAuthSalt()
    {
        var authSalt = new byte[16];
        Rng.GetBytes(authSalt);

        return authSalt;
    }

    public static string HashPassword(string password, byte[] salt)
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