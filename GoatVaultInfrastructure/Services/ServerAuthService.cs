using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace GoatVaultInfrastructure.Services;

public class ServerAuthService(
    HttpClient http,
    IConfiguration configuration) : IServerAuthService
{
    private string BaseUrl => configuration.GetSection("API_BASE_URL").Value
                              ?? throw new InvalidOperationException("Server base URL missing");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
        // DefaultIgnoreCondition removed. Individual DTOs control serialization.
    };

    public async Task<AuthInitResponse> InitAsync(AuthInitRequest payload, CancellationToken ct = default)
    {
        return await PostAsync<AuthInitResponse>("v1/auth/init", payload, ct);
    }

    public async Task<AuthVerifyResponse> VerifyAsync(AuthVerifyRequest payload, CancellationToken ct = default)
    {
        return await PostAsync<AuthVerifyResponse>("v1/auth/verify", payload, ct);
    }

    public async Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"{BaseUrl}/v1/users/{userId}", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<UserResponse>(json, JsonOptions) ?? throw new InvalidOperationException("Invalid user response");
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, object payload, CancellationToken ct = default)
    {
        return await PatchAsync<UserResponse>($"v1/users/{userId}", payload, ct);
    }

    public async Task<AuthRegisterResponse> RegisterAsync(AuthRegisterRequest payload, CancellationToken ct = default)
    {
        return await PostAsync<AuthRegisterResponse>("v1/auth/register", payload, ct);
    }

    private async Task<T> PostAsync<T>(string endpoint, object payload, CancellationToken ct)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var resp = await http.PostAsync($"{BaseUrl}/{endpoint}", content, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new InvalidOperationException($"Invalid response for {endpoint}");
    }

    private async Task<T> PatchAsync<T>(string endpoint, object payload, CancellationToken ct)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var resp = await http.PatchAsync($"{BaseUrl}/{endpoint}", content, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new InvalidOperationException($"Invalid response for {endpoint}");
    }
}