using GoatVaultClient_v3.Models;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Services
{
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
        public UserResponse User;
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public AuthRegisterRequest RegisterUser(string email, string masterPassword, VaultModel vault)
        {
            byte[] authSalt = new byte[16];
            Rng.GetBytes(authSalt);

            var authVerifier = HashPassword(masterPassword, authSalt);

            return new AuthRegisterRequest
            {
                AuthSalt = Convert.ToBase64String(authSalt),
                AuthVerifier = authVerifier,
                Email = email,
                Vault = vault,
            };

            //return JsonSerializer.Serialize(userPayload, new JsonSerializerOptions { WriteIndented = true });
        }

        // Used during login to generate the auth verifier to compare against stored value
        public string GenerateAuthVerifier(string masterPassword, string authSaltBase64)
        {
            // Convert the stored Base64 salt to byte[]
            byte[] authSalt = Convert.FromBase64String(authSaltBase64);

            // Hash the password using the salt
            string authVerifier = HashPassword(masterPassword, authSalt);

            return authVerifier;
        }

        private static string HashPassword(string password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

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
            using SecureArray<byte> hash = argon2.Hash();

            return Convert.ToBase64String(hash.Buffer);
        }
    }
}
