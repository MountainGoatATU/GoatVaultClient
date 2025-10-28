using GoatVaultClient.Models;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoatVaultClient.Helpers
{
    public class UserService
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public string RegisterUser(string email, string password)
        {
            byte[] salt = new byte[16];
            Rng.GetBytes(salt);

            var passwordHash = HashPassword(password, salt);

            var userPayload = new UserPayload
            {
                email = email,
                salt = Convert.ToBase64String(salt),
                password_hash = passwordHash,
                mfa_enabled = false,
                mfa_secret = ""
            };

            return JsonSerializer.Serialize(userPayload, new JsonSerializerOptions { WriteIndented = true });
        }

        public string LoginUser(string email, string password, string storedSalt, string storedPasswordHash)
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            var newHash = HashPassword(password, salt);

            bool isEqual = CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(newHash),
                Convert.FromBase64String(storedPasswordHash)
            );

            return isEqual ? "Login successful" : "Login failed";
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
