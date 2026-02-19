using GoatVaultCore.Models.Api;

namespace GoatVaultCore.Abstractions;

public interface IServerAuthService
{
    Task<AuthInitResponse> InitAsync(AuthInitRequest payload, CancellationToken ct = default);
    Task<AuthVerifyResponse> VerifyAsync(AuthVerifyRequest payload, CancellationToken ct = default);
    Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserResponse> UpdateUserAsync(Guid userId, object request, CancellationToken ct = default);
    Task<AuthRegisterResponse> RegisterAsync(AuthRegisterRequest authRegisterRequest, CancellationToken ct = default);
}