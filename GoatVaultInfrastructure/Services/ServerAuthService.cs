using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.API;
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
    };

    public async Task<AuthInitResponse> InitAsync(AuthInitRequest payload, CancellationToken ct = default) => await PostAsync<AuthInitResponse>("v1/auth/init", payload, ct);

    public async Task<AuthVerifyResponse> VerifyAsync(AuthVerifyRequest payload, CancellationToken ct = default) => await PostAsync<AuthVerifyResponse>("v1/auth/verify", payload, ct);

    public async Task<UserResponse> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"{BaseUrl}/v1/users/{userId}", ct);
        await EnsureSuccessAsync(response, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<UserResponse>(json, JsonOptions) ?? throw new InvalidOperationException("Invalid user response");
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, object payload, CancellationToken ct = default) => await PatchAsync<UserResponse>($"v1/users/{userId}", payload, ct);

    public async Task<AuthRegisterResponse> RegisterAsync(AuthRegisterRequest payload, CancellationToken ct = default) => await PostAsync<AuthRegisterResponse>("v1/auth/register", payload, ct);

    public async Task<string> DeleteUserAsync(Guid userId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{BaseUrl}/v1/users/{userId}", ct);
        await EnsureSuccessAsync(response, ct);
        var json = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        return JsonSerializer.Deserialize<string>(json, JsonOptions) ?? throw new InvalidOperationException("Invalid response");
    }

    private async Task<T> PostAsync<T>(string endpoint, object payload, CancellationToken ct)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        var resp = await http.PostAsync($"{BaseUrl}/{endpoint}", content, ct);
        await EnsureSuccessAsync(resp, ct);

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
        await EnsureSuccessAsync(resp, ct);

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new InvalidOperationException($"Invalid response for {endpoint}");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            try
            {
                var validationErrors = JsonSerializer.Deserialize<HttpValidationError>(content, JsonOptions);
                if (validationErrors != null)
                {
                    throw new ApiException("Validation Error", (int)response.StatusCode, validationErrors);
                }
            }
            catch (JsonException)
            {
                // Fallback to generic error if deserialization fails
            }
        }

        // Try to parse a generic 'detail' string from JSON if available
        var message = string.Empty;
        try
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("detail", out var detailElement))
                {
                    if (detailElement.ValueKind == JsonValueKind.String)
                        message = detailElement.GetString() ?? string.Empty;
                    else
                        message = detailElement.ToString();
                }
            }
        }
        catch
        {
            // If content is not JSON or doesn't have 'detail', use raw content or status
            message = content;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            // Fallback to the standard HttpRequestException format which users are familiar with
            message = $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";
        }
        else
        {
            // If we have a message (either extracted detail or raw content), append status code if not already present
            if (!message.Contains(((int)response.StatusCode).ToString()))
            {
                 message = $"{message} ({(int)response.StatusCode})";
            }
        }

        throw new ApiException(message, (int)response.StatusCode);
    }
}
