using GoatVaultCore.Abstractions;
using GoatVaultCore.Models.Api;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GoatVaultInfrastructure.Services.Api;

public class AuthenticatedHttpHandler(
    IAuthTokenService authTokenService,
    JwtUtils jwtUtils,
    string refreshUrl,
    ILogger<AuthenticatedHttpHandler>? logger = null)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = authTokenService.GetToken();

        if (logger != null)
        {
            var fragment = !string.IsNullOrEmpty(token) && token.Length > 10 ? token[..10] : "null/empty";
            logger.LogInformation("AuthenticatedHttpHandler processing request to {Uri}. Token: {TokenFragment}...", request.RequestUri, fragment);
        }

        if (!string.IsNullOrWhiteSpace(token) && jwtUtils.IsExpired(token))
        {
            logger?.LogInformation("Access token expired. Refreshing...");
            await RefreshTokenAsync();
            token = authTokenService.GetToken();
        }

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            throw new HttpRequestException("Server unreachable – use offline mode", ex);
        }
    }

    private async Task RefreshTokenAsync()
    {
        var refreshToken = authTokenService.GetRefreshToken();
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedAccessException("No refresh token available.");

        var payload = new AuthRefreshRequest { RefreshToken = refreshToken };

        using var client = new HttpClient();
        var response = await client.PostAsJsonAsync(refreshUrl, payload);

        if (!response.IsSuccessStatusCode)
            throw new UnauthorizedAccessException("Refresh token request failed");

        var refreshResponse = await response.Content.ReadFromJsonAsync<AuthRefreshResponse>();
        if (refreshResponse == null || string.IsNullOrWhiteSpace(refreshResponse.AccessToken))
            throw new InvalidOperationException("Refresh token response invalid or null");

        authTokenService.SetToken(refreshResponse.AccessToken);
        authTokenService.SetRefreshToken(refreshResponse.RefreshToken ?? string.Empty);

        logger?.LogInformation("Access token successfully refreshed.");
    }
}
