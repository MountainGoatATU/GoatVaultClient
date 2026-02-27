using GoatVaultCore.Models.Objects;

namespace GoatVaultCore.Abstractions;

public interface ICryptoService
{
    byte[] GenerateAuthVerifier(string password, byte[] authSalt, Argon2Parameters? parameters = null);
    MasterKey DeriveMasterKey(string password, byte[] vaultSalt, Argon2Parameters? parameters = null);
    byte[] GenerateSalt();
}
