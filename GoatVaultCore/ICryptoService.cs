using GoatVaultCore.Models;
using GoatVaultCore.Models.Vault;

namespace GoatVaultCore;

public interface ICryptoService
{
    byte[] GenerateAuthVerifier(string password, byte[] authSalt);
    MasterKey DeriveMasterKey(string password, byte[] vaultSalt);
    bool BytesEqual(byte[] left, byte[] right);
}
