using GoatVaultCore.Models;

namespace GoatVaultTests;

internal static class TestFixtures
{
    internal static VaultEncrypted CreateEncryptedVault() => new(
        encryptedBlob: [1, 2, 3],
        nonce: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
        authTag: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
    {
        EncryptedBlob = [1, 2, 3],
        Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
        AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
    };
}
