using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using GoatVaultInfrastructure.Database;
using GoatVaultInfrastructure.Services.API;
using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GoatVaultInfrastructure.Services.Vault;


public interface IVaultService
{
    VaultModel EncryptVault(string password, VaultData vaultData);
    VaultData DecryptVault(VaultModel vault, string password);
    Task SyncAndCloseAsync(UserResponse currentUser, string password, VaultData vaultData);
    Task SaveVaultAsync(UserResponse currentUser, string password, VaultData vaultData);
}

public class VaultService(IConfiguration configuration,GoatVaultDb goatVaultDb, HttpService httpService, VaultSessionService vaultSessionService) : IVaultService
{
    // Create a single, static, RandomNumberGenerator instance to be used throughout the application.
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    #region Vault Encryption/Decryption
    public VaultModel EncryptVault(string masterPassword, VaultData? vaultData)
    {
        // Create default vault structure if none exists
        vaultData ??= new VaultData
        {
            Categories =
            [
                "General",
                "Email",
                "Banking",
                "Social Media",
                "Work",
                "Entertainment"
            ],
            Entries = []
        };

        var vaultSalt = GenerateRandomBytes(16);
        var key = DeriveKey(masterPassword, vaultSalt);

        var vaultJson = JsonSerializer.Serialize(vaultData, JsonOptions);
        var plaintextBytes = Encoding.UTF8.GetBytes(vaultJson);

        // Encrypt using AES-256-GCM
        var nonce = GenerateRandomBytes(12);
        var ciphertext = new byte[plaintextBytes.Length];
        var authTag = new byte[16];

        using (var aesGcm = new AesGcm(key, 16))
        {
            // Encrypt the plaintext
            aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, authTag);
        }

        // Prepare payload for server
        return new VaultModel
        {
            VaultSalt = Convert.ToBase64String(vaultSalt),
            Nonce = Convert.ToBase64String(nonce),
            EncryptedBlob = Convert.ToBase64String(ciphertext),
            AuthTag = Convert.ToBase64String(authTag)
        };

        // return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    public VaultData DecryptVault(VaultModel payload, string password)
    {
        ArgumentNullException.ThrowIfNull(payload);

        try
        {
            // Decode Base64 fields
            var vaultSalt = Convert.FromBase64String(payload.VaultSalt ?? throw new Exception());
            var nonce = Convert.FromBase64String(payload.Nonce ?? throw new Exception());
            var ciphertext = Convert.FromBase64String(payload.EncryptedBlob ?? throw new Exception());
            var authTag = Convert.FromBase64String(payload.AuthTag ?? throw new Exception());

            // Derive encryption key
            var key = DeriveKey(password, vaultSalt);

            // Decrypt
            var decryptedBytes = new byte[ciphertext.Length];
            using (var aesGcm = new AesGcm(key, 16))
            {
                aesGcm.Decrypt(nonce, ciphertext, authTag, decryptedBytes);
            }

            // Convert decrypted bytes to JSON
            var decryptedJson = Encoding.UTF8.GetString(decryptedBytes);

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

    public async Task SyncAndCloseAsync(UserResponse? user, string password, VaultData? vaultData)
    {
        await SaveVaultAsync(user, password, vaultData);
        vaultSessionService.Lock();
    }

    public async Task SaveVaultAsync(UserResponse? user, string password, VaultData? vaultData)
    {
        var url = configuration.GetSection("GOATVAULT_SERVER_BASE_URL").Value;

        if (user == null || string.IsNullOrEmpty(password) || vaultData == null)
            return;

        try
        {
            // Encrypt the vault data
            var encryptedModel = EncryptVault(password, vaultData);
            var now = DateTime.UtcNow;

            // Check if user exists in local DB
            var existingUser = await goatVaultDb.LocalCopy.FirstOrDefaultAsync(u => u.Id == user.Id);

            if (existingUser != null)
            {
                // Update existing user
                existingUser.Vault = encryptedModel;
                existingUser.UpdatedAt = now;
                existingUser.Email = user.Email;
                existingUser.MfaEnabled = user.MfaEnabled;
                goatVaultDb.LocalCopy.Update(existingUser);
            }
            else
            {
                // Create new user in local DB
                var dbModel = new DbModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    AuthSalt = user.AuthSalt,
                    MfaEnabled = user.MfaEnabled,
                    Vault = encryptedModel,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = now
                };
                goatVaultDb.LocalCopy.Add(dbModel);
            }

            // Save to local database first
            await goatVaultDb.SaveChangesAsync();

            // Then try to sync with server if online
            try
            {
                var userRequest = new UserRequest
                {
                    Email = user.Email,
                    MfaEnabled = user.MfaEnabled,
                    Vault = encryptedModel
                };

                var userResponse = await httpService.PatchAsync<UserResponse>(
                    $"{url}v1/users/{user.Id}",
                    userRequest
                );

                // Update local with server timestamps after successful sync
                if (existingUser != null)
                {
                    existingUser.UpdatedAt = userResponse.UpdatedAt;
                    await goatVaultDb.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // If server sync fails, that's okay - we have it saved locally
                System.Diagnostics.Debug.WriteLine($"Server sync failed (offline mode): {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving vault: {ex}");
            throw;
        }
    }

    #endregion
    #region Sync with server
    #endregion 
    #region Local Storage
    // GET
    public async Task<DbModel?> LoadUserFromLocalAsync(string userId)
    {
        return await goatVaultDb.LocalCopy
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<List<DbModel>> LoadAllUsersFromLocalAsync()
    {
        return await goatVaultDb.LocalCopy.ToListAsync();
    }

    // POST
    public async Task SaveUserToLocalAsync(DbModel user)
    {
        goatVaultDb.LocalCopy.Add(user);
        await goatVaultDb.SaveChangesAsync();
    }

    // PUT/PATCH
    public async Task UpdateUserInLocalAsync(DbModel user)
    {
        goatVaultDb.LocalCopy.Update(user);
        await goatVaultDb.SaveChangesAsync();
    }

    // DELETE
    public async Task DeleteUserFromLocalAsync(string userId)
    {
        var user = await goatVaultDb.LocalCopy
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user != null)
        {
            goatVaultDb.LocalCopy.Remove(user);
            await goatVaultDb.SaveChangesAsync();
        }
    }
    #endregion
    #region Helper Methods
    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        Rng.GetBytes(bytes);
        return bytes;
    }

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        // Derive 32-byte key using Argon2id
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
        return (byte[])hash.Buffer.Clone();
    }
    #endregion
}