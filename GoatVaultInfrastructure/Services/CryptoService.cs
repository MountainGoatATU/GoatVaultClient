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

    public byte[] GenerateAuthVerifier(string password, byte[] authSalt, Argon2Parameters? parameters = null)
        => Argon2Hash(password, authSalt, parameters ?? Argon2Parameters.Default);

    public MasterKey DeriveMasterKey(string password, byte[] vaultSalt, Argon2Parameters? parameters = null)
        => new(Argon2Hash(password, vaultSalt, parameters ?? Argon2Parameters.Default));

    byte[] ICryptoService.GenerateSalt() => GenerateSalt();

    public static byte[] GenerateSalt() => GenerateRandomBytes(SaltLength);

    public static byte[] GenerateNonce() => GenerateRandomBytes(NonceLength);

    private static byte[] Argon2Hash(string password, byte[] salt, Argon2Parameters parameters)
    {
        var passwordBytes = Encoding.UTF8.GetBytes(password);

        var lanes = Math.Min(parameters.Lanes, Environment.ProcessorCount);
        var threads = Math.Min(parameters.Threads, Environment.ProcessorCount);

        var config = new Argon2Config
        {
            Type = Argon2Type.HybridAddressing,
            Version = Argon2Version.Nineteen,
            TimeCost = parameters.TimeCost,
            MemoryCost = parameters.MemoryCost,
            Lanes = lanes,
            Threads = threads,
            Password = passwordBytes,
            Salt = salt,
            HashLength = parameters.HashLength,
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
