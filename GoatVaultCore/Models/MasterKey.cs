using System.Security.Cryptography;

namespace GoatVaultCore.Models;

public sealed class MasterKey(byte[] key) : IDisposable
{
    public byte[] Key { get; } = key ?? throw new ArgumentNullException(nameof(key));

    public void Dispose() => CryptographicOperations.ZeroMemory(Key);
}