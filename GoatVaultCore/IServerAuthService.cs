using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;

namespace GoatVaultCore;

public interface IServerAuthService
{
    Task<AuthInitResponse> InitAsync(Email email, CancellationToken ct = default);
    Task<AuthVerifyResponse> VerifyAsync(Guid userId, byte[] authVerifier, string? mfaCode = null, CancellationToken ct = default);
    Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<AuthRegisterResponse> RegisterAsync(
        Email email,
        byte[] authSalt,
        byte[] authVerifier,
        byte[] vaultSalt,
        VaultEncrypted? vault,
        CancellationToken ct = default);
}