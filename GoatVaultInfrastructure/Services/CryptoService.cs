using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Objects;
using Isopoh.Cryptography.Argon2;
using Isopoh.Cryptography.SecureArray;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultInfrastructure.Services;

public sealed class CryptoService : ICryptoService
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
    private const int SaltLength = 32;
    private const int NonceLength = 12;

    public byte[] GenerateAuthVerifier(string password, byte[] authSalt) => Argon2Hash(password, authSalt);

    public MasterKey DeriveMasterKey(string password, byte[] vaultSalt) => new(Argon2Hash(password, vaultSalt));

    public static byte[] GenerateSalt() => GenerateRandomBytes(SaltLength);

    public static byte[] GenerateNonce() => GenerateRandomBytes(NonceLength);

    private static byte[] Argon2Hash(string password, byte[] salt)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var lanes = Math.Min(4, Environment.ProcessorCount);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = 3,
            MemoryCost = 65536,
            Lanes = lanes,
            Threads = lanes,
            Password = passwordBytes,
            Salt = salt,
            HashLength = 32,
        };

        SecureArray.ReportMaxLockableOnLockFail = true;

        using var argon2 = new Argon2(config);
        using var hash = argon2.Hash();
        var result = hash.Buffer.ToArray();
        CryptographicOperations.ZeroMemory(passwordBytes);
        return result;
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        Rng.GetBytes(bytes);
        return bytes;
    }
}
