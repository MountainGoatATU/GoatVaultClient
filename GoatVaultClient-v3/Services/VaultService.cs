using GoatVaultClient_v3.Database;
using GoatVaultClient_v3.Models;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Services
{
    public interface IVaultService
    {
        VaultModel EncryptVault(string password, VaultData vaultData);
        VaultData DecryptVault(VaultModel vault, string password);
        Task SyncAndCloseAsync(UserResponse currentUser, string password, VaultData vaultData);
    }
    public class VaultService(GoatVaultDB goatVaultDB, HttpService httpService, VaultSessionService vaultSessionService) : IVaultService
    {
        private readonly GoatVaultDB _goatVaultDB = goatVaultDB;
        private readonly HttpService _httpService = httpService;
        private readonly VaultSessionService _vaultSessionService = vaultSessionService;

        // Create a single, static, RandomNumberGenerator instance to be used throughout the application.
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #region Vault Encryption/Decryption
        public VaultModel EncryptVault(string masterPassword, VaultData vaultData)
        {
            // Create default vault structure if none exists
            if (vaultData == null)
            {
                vaultData = new VaultData
                {
                    Categories = new List<string>()
                    {
                        "General",
                        "Email",
                        "Banking",
                        "Social Media",
                        "Work",
                        "Entertainment"
                    },
                    Entries = new List<VaultEntry>()
                };
            }

            byte[] vault_salt = GenerateRandomBytes(16);
            byte[] key = DeriveKey(masterPassword, vault_salt);

            string vaultJson = JsonSerializer.Serialize(vaultData, JsonOptions);
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
            return new VaultModel
            {
                VaultSalt = Convert.ToBase64String(vault_salt),
                Nonce = Convert.ToBase64String(nonce),
                EncryptedBlob = Convert.ToBase64String(ciphertext),
                AuthTag = Convert.ToBase64String(authTag)
            };

            //return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }

        public VaultData DecryptVault(VaultModel payload, string password)
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            try
            {
                // Decode Base64 fields
                byte[] vaultSalt = Convert.FromBase64String(payload.VaultSalt);
                byte[] nonce = Convert.FromBase64String(payload.Nonce);
                byte[] ciphertext = Convert.FromBase64String(payload.EncryptedBlob);
                byte[] authTag = Convert.FromBase64String(payload.AuthTag);

                // Derive encryption key
                byte[] key = DeriveKey(password, vaultSalt);

                // Decrypt
                byte[] decryptedBytes = new byte[ciphertext.Length];
                using (var aesGcm = new AesGcm(key, 16))
                {
                    aesGcm.Decrypt(nonce, ciphertext, authTag, decryptedBytes);
                }

                // Convert decrypted bytes to JSON
                string decryptedJson = Encoding.UTF8.GetString(decryptedBytes);

                // Deserialize JSON into VaultData
                var vaultData = JsonSerializer.Deserialize<VaultData>(decryptedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException("Failed to deserialize decrypted vault.");

                return vaultData;
            }
            catch (CryptographicException)
            {
                throw new InvalidOperationException("Decryption failed — incorrect password or data tampered.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error while decrypting vault: {ex.Message}", ex);
            }
        }

        public async Task SyncAndCloseAsync(UserResponse user, string password, VaultData vaultData)
        {
            if (user == null || string.IsNullOrEmpty(password) || vaultData == null)
                return;

            try
            {
                //Encrypt the vault data
                VaultModel encryptedModel = EncryptVault(password, vaultData);

                //Model for local DB
                var dbModel = new DbModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    AuthSalt = user.AuthSalt,
                    MfaEnabled = user.MfaEnabled,
                    Vault = encryptedModel,
                    UpdatedAt = DateTime.UtcNow
                };

                //Model for server sync
                var userRequest = new UserRequest
                {
                    Email = user.Email,
                    MfaEnabled = user.MfaEnabled,
                    Vault = encryptedModel
                };

                var existingUser = await _goatVaultDB.LocalCopy.FirstOrDefaultAsync(u => u.Id == user.Id);
                if (existingUser != null)
                {
                    //Update local Vault
                    existingUser.Vault = dbModel.Vault;
                    _goatVaultDB.LocalCopy.Update(existingUser);

                    //Sync with server
                    var userResponse = await _httpService.PatchAsync<UserResponse>(
                    $"http://127.0.0.1:8000/v1/users/{_vaultSessionService.CurrentUser.Id}",
                    userRequest
                );

                }
                else
                {
                    // Create local vault
                    _goatVaultDB.LocalCopy.Add(dbModel);
                }

                await _goatVaultDB.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error syncing on close: {ex.Message}");
            }
        }
        #endregion

        #region Local Storage
        // GET
        public async Task<DbModel?> LoadUserFromLocalAsync(string userId)
        {
            return await _goatVaultDB.LocalCopy
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        // POST
        public async Task SaveUserToLocalAsync(DbModel user)
        {
            _goatVaultDB.LocalCopy.Add(user);
            await _goatVaultDB.SaveChangesAsync();
        }

        // PUT/PATCH
        public async Task UpdateUserInLocalAsync(DbModel user)
        {
            _goatVaultDB.LocalCopy.Update(user);
            await _goatVaultDB.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteUserFromLocalAsync(string userId)
        {
            var user = await _goatVaultDB.LocalCopy
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                _goatVaultDB.LocalCopy.Remove(user);
                await _goatVaultDB.SaveChangesAsync();
            }
        }
        #endregion

        #region Helper Methods
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
        #endregion
    }
}
