using GoatVaultCore.Models.API;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GoatVaultCore.Abstractions;

namespace GoatVaultInfrastructure.Services.API;

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

        if (!string.IsNullOrWhiteSpace(token) && jwtUtils.IsExpired(token))
        {
            logger?.LogInformation("Access token expired. Refreshing...");
            await RefreshTokenAsync();
            token = authTokenService.GetToken();
        }

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
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
