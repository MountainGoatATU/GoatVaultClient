using GoatVaultCore.Models.API;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GoatVaultInfrastructure.Services.API;

public interface IHttpService
{
    // Public methods for making HTTP requests
    Task<T> GetAsync<T>(string url);
    Task<T> PostAsync<T>(string url, object payload);
    Task<T> PatchAsync<T>(string url, object payload);
    Task<T> DeleteAsync<T>(string url);
}

// Use of primary constructor to inject HttpClient dependency
public class HttpService(HttpClient client, AuthTokenService authTokenService,JwtUtils jwtUtils, IConfiguration configuration) : IHttpService
{
    private readonly HttpClient _client = client;
    private readonly AuthTokenService _authTokenService = authTokenService;
    private readonly JwtUtils _jwtUtils = jwtUtils;

    // JSON serialization options
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? payload = null)
    {
        try
        {
            // Create the HTTP request based on the method and URL
            using var request = new HttpRequestMessage(method, url);

            // Attach Authorization header if token exists
            var token = _authTokenService.GetToken();
            // If no token is found, throw an exception to indicate the user needs to log in
            if (token != null)
            {
                // Convert token string to JwtSecurityToken to check expiration
                var convertedToken = _jwtUtils.ConvertJwtStringToJwtSecurityToken(token);
                // Decode the token to extract claims and other info
                var decodedToken = _jwtUtils.DecodeToken(convertedToken);
                // Check if the token has expired
                if (decodedToken.Expiration < DateTime.UtcNow)
                {
                    // Clear access token
                    _authTokenService.ClearToken();
                    // Call refresh token endpoint to get new tokens
                    var refreshPayload = new
                    {
                        refresh_token = _authTokenService.GetRefreshToken()
                    };
                    var refreshResponse = await PostAsync<AuthRefreshResponse>(
                        $"{url}v1/auth/refresh",
                        refreshPayload
                    );
                    // Update tokens in AuthTokenService
                    if (refreshResponse != null)
                    {
                        _authTokenService.SetToken(refreshResponse.AccessToken);
                        _authTokenService.SetRefreshToken(refreshResponse.RefreshToken);
                        token = refreshResponse.AccessToken; // Update token variable for use in header
                    }
                }
            }
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // If there's a payload, serialize it to JSON and add it to the request body
            if (payload != null)
            {
                string json;

                // Don't double serialize if payload is already a string
                if (payload is string jsonString)
                    json = jsonString;
                else
                    json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            var response = await _client.SendAsync(request);

            // Check for HTTP error responses
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();

                // Try to extract a meaningful error message from the response
                string errorMessage;
                try
                {
                    // Try to parse as JSON and extract message
                    var errorJson = JsonSerializer.Deserialize<JsonElement>(errorBody);
                    if (errorJson.TryGetProperty("message", out var msgProperty))
                    {
                        errorMessage = msgProperty.GetString() ?? errorBody;
                    }
                    else if (errorJson.TryGetProperty("error", out var errProperty))
                    {
                        errorMessage = errProperty.GetString() ?? errorBody;
                    }
                    else
                    {
                        errorMessage = errorBody;
                    }
                }
                catch
                {
                    errorMessage = errorBody;
                }

                // Throw HttpRequestException with StatusCode preserved
                throw new HttpRequestException(
                    errorMessage,
                    null,
                    response.StatusCode);
            }

            // Read the response content as a string
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<T>(responseString, JsonOptions)
                 ?? throw new InvalidOperationException(
                     $"Failed to deserialize response into {typeof(T).Name}. Raw: {responseString}");

            return result;
        }
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            // A TaskCanceledException without user cancellation => timeout
            throw new TimeoutException($"Request timed out after {_client.Timeout.TotalSeconds} seconds.", ex);
        }
        catch (HttpRequestException)
        {
            // Re-throw HttpRequestException to preserve StatusCode
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected errors - provide generic message without exposing URL
            throw new Exception("Unexpected error while sending request.", ex);
        }
    }

    public Task<T> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);
    public Task<T> PostAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Post, url, payload);
    public Task<T> PatchAsync<T>(string url, object payload) => SendAsync<T>(HttpMethod.Patch, url, payload);
    public Task<T> DeleteAsync<T>(string url) => SendAsync<T>(HttpMethod.Delete, url);
}