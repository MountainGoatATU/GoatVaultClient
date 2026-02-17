using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace GoatVaultInfrastructure.Services.API;

public class HttpService(
    HttpClient client,
    ILogger<HttpService>? logger = null)
    : IHttpService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? payload = null)
    {
        using var request = new HttpRequestMessage(method, url);

        if (payload != null)
            request.Content = JsonContent.Create(payload);

        var stopwatch = Stopwatch.StartNew();
        var response = await client.SendAsync(request);
        stopwatch.Stop();

        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return JsonSerializer.Deserialize<T>(content, JsonOptions)
                   ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}");
        }

        logger?.LogWarning("HTTP {Method} {Url} failed {StatusCode} in {Elapsed}ms",
            method, url, response.StatusCode, stopwatch.ElapsedMilliseconds);

        throw new HttpRequestException($"Request failed: {content}", null, response.StatusCode);
    }

    public Task<T> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    public Task<T> PostAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Post, url, payload);
    public Task<T> PatchAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Patch, url, payload);
    public Task<T> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url);
}