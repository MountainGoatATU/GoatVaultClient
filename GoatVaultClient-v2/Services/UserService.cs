using GoatVaultClient_v2.Models;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v2.Services
{
    public enum LoginStatus
    {
        Success,
        Error
    }
    public interface IUserService
    {
        UserPayload RegisterUser(string email, string password);
        LoginStatus LoginUser(string email, string password, string storedSalt, string storedPasswordHash);
    }
    internal class UserService : IUserService
    {
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        public UserPayload RegisterUser(string email, string password)
        {
            byte[] salt = new byte[16];
            Rng.GetBytes(salt);

            var passwordHash = HashPassword(password, salt);

            return new UserPayload
            {
                Email = email,
                Salt = Convert.ToBase64String(salt),
                Password_hash = passwordHash,
                Mfa_enabled = false,
                Mfa_secret = ""
            };

            //return JsonSerializer.Serialize(userPayload, new JsonSerializerOptions { WriteIndented = true });
        }

        public LoginStatus LoginUser(string email, string password, string storedSalt, string storedPasswordHash)
        {
            byte[] salt = Convert.FromBase64String(storedSalt);
            var newHash = HashPassword(password, salt);

            bool isEqual = CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(newHash),
                Convert.FromBase64String(storedPasswordHash)
            );

            return isEqual ? LoginStatus.Success : LoginStatus.Error;
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
