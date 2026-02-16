using GoatVaultCore;
using GoatVaultCore.Models;
using GoatVaultCore.Models.API;
using GoatVaultCore.Models.Vault;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace GoatVaultInfrastructure;

public class ServerAuthService(
    HttpClient http,
    IConfiguration configuration) : IServerAuthService
{
    private string BaseUrl => configuration.GetSection("API_BASE_URL").Value
                              ?? throw new InvalidOperationException("Server base URL missing");

    public async Task<AuthInitResponse> InitAsync(Email email, CancellationToken ct = default)
    {
        var payload = new AuthInitRequest { Email = email.Value };

        return await PostAsync<AuthInitResponse>("v1/auth/init", payload, ct);
    }

    public async Task<AuthVerifyResponse> VerifyAsync(Guid userId, byte[] authVerifier, string? mfaCode = null, CancellationToken ct = default)
    {
        var payload = new AuthVerifyRequest
        {
            UserId = userId,
            AuthVerifier = Convert.ToBase64String(authVerifier),
            MfaCode = mfaCode
        };

        return await PostAsync<AuthVerifyResponse>("v1/auth/verify", payload, ct);
    }

    public async Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"{BaseUrl}/v1/users/{userId}", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<UserResponse>(json) ?? throw new InvalidOperationException("Invalid user response");
    }

    public async Task<AuthRegisterResponse> RegisterAsync(
        Email email,
        byte[] authSalt,
        byte[] authVerifier,
        byte[] vaultSalt,
        VaultEncrypted? vault,
        CancellationToken ct = default)
    {
        var payload = new AuthRegisterRequest
        {
            Email = email.Value,
            AuthSalt = Convert.ToBase64String(authSalt),
            AuthVerifier = Convert.ToBase64String(authVerifier),
            VaultSalt = Convert.ToBase64String(vaultSalt),
            Vault = vault
        };

        return await PostAsync<AuthRegisterResponse>("v1/auth/register", payload, ct);
    }

    private async Task<T> PostAsync<T>(string endpoint, object payload, CancellationToken ct)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        var resp = await http.PostAsync($"{BaseUrl}/{endpoint}", content, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException($"Invalid response for {endpoint}");
    }
}