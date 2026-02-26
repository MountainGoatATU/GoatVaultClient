using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GoatVaultInfrastructure.Services.Api.Models;

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

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, url);

        if (payload != null)
        {
            request.Content = JsonContent.Create(payload, options: JsonOptions);
        }

        var stopwatch = Stopwatch.StartNew();
        var response = await client.SendAsync(request);
        stopwatch.Stop();

        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            await Task.Run(() =>
            {
                return JsonSerializer.Deserialize<T>(content, JsonOptions)
                   ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
            });
        }

        logger?.LogWarning("HTTP {Method} {Url} failed {StatusCode} in {Elapsed}ms",
            method, url, response.StatusCode, stopwatch.ElapsedMilliseconds);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
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
        }

        throw new ApiException($"Request failed: {content}", (int)response.StatusCode);
    }

    public Task<T> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    public Task<T> PostAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Post, url, payload);
    public Task<T> PatchAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Patch, url, payload);
    public Task<T> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url);
}