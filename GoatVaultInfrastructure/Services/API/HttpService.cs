using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.API;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GoatVaultInfrastructure.Services.Api;

public class HttpService(
    HttpClient client,
    ILogger<HttpService>? logger = null)
    : IHttpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? payload = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, url);

        if (payload != null)
        {
            request.Content = JsonContent.Create(payload, options: JsonOptions);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await client.SendAsync(request, ct);
        stopwatch.Stop();

        var content = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                return JsonSerializer.Deserialize<T>(content, JsonOptions)
                       ?? throw new InvalidOperationException($"Invalid API response payload for {method} {url}.");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid API response payload for {method} {url}.", ex);
            }
        }

        logger?.LogWarning("HTTP {Method} {Url} failed {StatusCode} in {Elapsed}ms",
            method, url, response.StatusCode, stopwatch.ElapsedMilliseconds);

        if (response.StatusCode != HttpStatusCode.UnprocessableEntity)
            throw new ApiException($"Request failed: {content}", (int)response.StatusCode);

        HttpValidationError? validationErrors = null;
        try
        {
            validationErrors = JsonSerializer.Deserialize<HttpValidationError>(content, JsonOptions);
        }
        catch
        {
            // Ignore deserialization errors
        }

        if (validationErrors != null)
        {
            throw new ApiException("Validation Error", (int)response.StatusCode, validationErrors);
        }

        throw new ApiException($"Request failed: {content}", (int)response.StatusCode);
    }

    public Task<T> GetAsync<T>(string url, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Get, url, ct: ct);
    public Task<T> PostAsync<T>(string url, object payload, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Post, url, payload, ct);
    public Task<T> PatchAsync<T>(string url, object payload, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Patch, url, payload, ct);
    public Task<T> DeleteAsync<T>(string url, CancellationToken ct = default) => SendAsync<T>(HttpMethod.Delete, url, ct: ct);
}
