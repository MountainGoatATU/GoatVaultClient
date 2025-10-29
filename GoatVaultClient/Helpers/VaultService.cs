using GoatVaultClient.DB;
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
    public class VaultService
    {
        // Create a single, static, RandomNumberGenerator instance to be used throughout the application.
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
        private readonly VaultDB _vaultDB;

        public VaultService(VaultDB vaultDB)
        {
            _vaultDB = vaultDB;
        }

        public VaultPayload CreateVault(string password)
        {
            byte[] salt = GenerateRandomBytes(16);
            byte[] key = DeriveKey(password, salt);

            // Example vault content (JSON)
            var vaultData = new
            {
                entries = new[] {
                    new { site = "github.com", username = "alice", password = "ghp_secret" },
                    new { site = "gmail.com", username = "bob", password = "gmail_secret" }
                }
            };

            string vaultJson = JsonSerializer.Serialize(vaultData, new JsonSerializerOptions { WriteIndented = true });
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(vaultJson);

            // Encrypt using AES-256-GCM
            byte[] nonce = GenerateRandomBytes(12);
            byte[] ciphertext = new byte[plaintextBytes.Length];
            byte[] authTag = new byte[16];

            using (var aesGcm = new AesGcm(key, 16))
            {
                // Encrypt the plaintext
                aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, authTag);
            }

            // Prepare payload for server
            return new VaultPayload
            {
                _id = "ae7f31ae-dbdf-46d5-8f48-c613e2117fbd",
                user_id = "b1c1f27a-cc59-4d2b-ae74-7b3b0e33a61a",
                name = "Testing Vault",
                salt = Convert.ToBase64String(salt),
                nonce = Convert.ToBase64String(nonce),
                encrypted_blob = Convert.ToBase64String(ciphertext),
                auth_tag = Convert.ToBase64String(authTag),
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            //return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }

        public void DecryptVaultFromServer(VaultPayload payload, string password)
        {
            try
            {
                if (payload == null)
                {
                    Console.WriteLine("Vault is null!");
                    return;
                }

                // Decode base64 fields
                byte[] salt = Convert.FromBase64String(payload.salt);
                byte[] nonce = Convert.FromBase64String(payload.nonce);
                byte[] ciphertext = Convert.FromBase64String(payload.encrypted_blob);
                byte[] authTag = Convert.FromBase64String(payload.auth_tag);

                // Derive the same key from password and salt
                byte[] key = DeriveKey(password, salt);

                // Attempt to decrypt
                byte[] decryptedBytes = new byte[ciphertext.Length];
                using (var aesGcm = new AesGcm(key, 16))
                {
                    aesGcm.Decrypt(nonce, ciphertext, authTag, decryptedBytes);
                }

                string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);
                Console.WriteLine("Decryption successful!");
                Console.WriteLine(decryptedJson);
            }
            catch (CryptographicException)
            {
                Console.WriteLine("Decryption failed — incorrect password or data tampered.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }

        public async Task SaveVaultToLocalAsync(VaultPayload vault)
        {
            // Add the vault to the DbContext
            _vaultDB.Vaults.Add(vault);

            // Save changes to the SQLite database
            await _vaultDB.SaveChangesAsync();
        }

        private static byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            Rng.GetBytes(bytes);
            return bytes;
        }

        private static byte[] DeriveKey(string password, byte[] salt)
        {
            // Derive 32-byte key using Argon2id
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
            return (byte[])hash.Buffer.Clone();
        }
    }
}

