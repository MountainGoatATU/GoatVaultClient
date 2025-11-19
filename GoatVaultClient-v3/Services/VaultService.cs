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
        VaultPayload EncryptVault(string password, VaultData vaultData);
        void DecryptVault(VaultPayload vault, string password);
        Task SaveVaultToLocalAsync(VaultPayload vault);
        Task<VaultPayload> LoadVaultFromLocalAsync(string vaultId);
    }
    public class VaultService(VaultDB vaultDB) : IVaultService
    {
        private readonly VaultDB _vaultDB = vaultDB;
    
        // Create a single, static, RandomNumberGenerator instance to be used throughout the application.
        private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        #region Vault Encryption/Decryption
        public VaultPayload EncryptVault(string masterPassword, VaultData vaultData)
        {
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
            return new VaultPayload
            {
                Vault_Salt = Convert.ToBase64String(vault_salt),
                Nonce = Convert.ToBase64String(nonce),
                EncryptedBlob = Convert.ToBase64String(ciphertext),
                AuthTag = Convert.ToBase64String(authTag)
            };

            //return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        }

        public void DecryptVault(VaultPayload payload, string password)
        {
            try
            {
                if (payload == null)
                {
                    Console.WriteLine("Vault is null!");
                    return;
                }

                // Decode base64 fields
                byte[] vault_salt = Convert.FromBase64String(payload.Vault_Salt);
                byte[] nonce = Convert.FromBase64String(payload.Nonce);
                byte[] ciphertext = Convert.FromBase64String(payload.EncryptedBlob);
                byte[] authTag = Convert.FromBase64String(payload.AuthTag);

                // Derive the same key from password and salt
                byte[] key = DeriveKey(password, vault_salt);

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
        #endregion

        #region Local Storage
        // GET all vaults
        // Retrieve all vaults from local SQLite database -> which means all the vaults for the current user
        public async Task<List<VaultPayload>> LoadAllVaultsFromLocalAsync()
        {
            var vaults = await _vaultDB.Vaults.ToListAsync();
            return vaults;
        }

        // GET vault by ID
        public async Task<VaultPayload> LoadVaultFromLocalAsync(string vaultId)
        {
            var vault = await _vaultDB.Vaults
                .FirstOrDefaultAsync(v => v.Id == vaultId);

            return vault;
        }

        // POST
        public async Task SaveVaultToLocalAsync(VaultPayload vault)
        {
            // Add the vault to the DbContext
            _vaultDB.Vaults.Add(vault);

            // Save changes to the SQLite database
            await _vaultDB.SaveChangesAsync();
        }

        // PUT/PATCH
        public async Task UpdateVaultInLocalAsync(VaultPayload vault)
        {
            // Update the vault in the DbContext
            _vaultDB.Vaults.Update(vault);
            // Save changes to the SQLite database
            await _vaultDB.SaveChangesAsync();
        }

        // DELETE
        public async Task DeleteVaultFromLocalAsync(string vaultId)
        {
            var vault = await _vaultDB.Vaults
                .FirstOrDefaultAsync(v => v.Id == vaultId);
            if (vault != null)
            {
                _vaultDB.Vaults.Remove(vault);
                await _vaultDB.SaveChangesAsync();
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
