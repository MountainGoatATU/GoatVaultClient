using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Objects;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultInfrastructure.Services;

public sealed class CryptoService : ICryptoService
{
    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();

    public byte[] GenerateAuthVerifier(string password, byte[] authSalt) => Argon2Hash(password, authSalt);

    public MasterKey DeriveMasterKey(string password, byte[] vaultSalt) => new(Argon2Hash(password, vaultSalt));

    private static byte[] Argon2Hash(string password, byte[] salt)
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

        return hash.Buffer.ToArray();
    }

    public static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        Rng.GetBytes(bytes);
        return bytes;
    }
}
