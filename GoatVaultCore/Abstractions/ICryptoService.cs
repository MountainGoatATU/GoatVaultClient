using GoatVaultCore.Models.Objects;

namespace GoatVaultCore.Abstractions;

public interface ICryptoService
{
    byte[] GenerateAuthVerifier(string password, byte[] authSalt);
    MasterKey DeriveMasterKey(string password, byte[] vaultSalt);
}
