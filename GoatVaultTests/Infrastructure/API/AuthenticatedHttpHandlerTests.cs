using GoatVaultInfrastructure.Services.Api;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;

namespace GoatVaultTests.Infrastructure.API;

public class AuthenticatedHttpHandlerTests
{
    private const string TestUri = "https://api.test/resource";
    private const string RefreshUri = "https://refresh.local/token";

    [Fact]
    public async Task SendAsync_WithValidToken_AddsBearerAuthorizationHeader()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var authTokenService = new AuthTokenService();
        authTokenService.SetToken(CreateToken(DateTime.UtcNow.AddMinutes(10)));

        var innerHandler = new CaptureHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            }));

        var authHandler = new AuthenticatedHttpHandler(
            authTokenService,
            new JwtUtils(),
            refreshUrl: RefreshUri)
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(authHandler);

        // Act
        await client.GetAsync(TestUri, ct);

        // Assert
        Assert.NotNull(innerHandler.LastRequest);
        Assert.NotNull(innerHandler.LastRequest!.Headers.Authorization);
        Assert.Equal("Bearer", innerHandler.LastRequest.Headers.Authorization!.Scheme);
        Assert.Equal(authTokenService.GetToken(), innerHandler.LastRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithNoToken_DoesNotSetAuthorizationHeader()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var authTokenService = new AuthTokenService();

        var innerHandler = new CaptureHandler(_ =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            }));

        var authHandler = new AuthenticatedHttpHandler(
            authTokenService,
            new JwtUtils(),
            refreshUrl: RefreshUri)
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(authHandler);

        // Act
        await client.GetAsync(TestUri, ct);

        // Assert
        Assert.NotNull(innerHandler.LastRequest);
        Assert.Null(innerHandler.LastRequest!.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_WithExpiredTokenAndNoRefreshToken_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var authTokenService = new AuthTokenService();
        authTokenService.SetToken(CreateToken(DateTime.UtcNow.AddMinutes(-10)));

        var authHandler = new AuthenticatedHttpHandler(
            authTokenService,
            new JwtUtils(),
            refreshUrl: RefreshUri)
        {
            InnerHandler = new CaptureHandler(_ =>
                Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("ok")
                }))
        };

        using var client = new HttpClient(authHandler);

        // Act + Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await client.GetAsync(TestUri, ct));
    }

    [Fact]
    public async Task SendAsync_WhenInnerSocketExceptionThrown_WrapsWithOfflineModeMessage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var authTokenService = new AuthTokenService();
        authTokenService.SetToken(CreateToken(DateTime.UtcNow.AddMinutes(10)));

        var authHandler = new AuthenticatedHttpHandler(
            authTokenService,
            new JwtUtils(),
            refreshUrl: RefreshUri)
        {
            InnerHandler = new CaptureHandler(_ => throw new HttpRequestException("network", new SocketException((int)SocketError.HostUnreachable)))
        };

        using var client = new HttpClient(authHandler);

        // Act
        var ex = await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.GetAsync(TestUri, ct));

        // Assert
        Assert.Contains("offline mode", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ex.InnerException);
    }

    private static string CreateToken(DateTime expiresUtc)
    {
        var token = new JwtSecurityToken(
            issuer: "goatvault-tests",
            audience: "goatvault-tests",
            notBefore: expiresUtc.AddMinutes(-1),
            expires: expiresUtc);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class CaptureHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return sendAsync(request);
        }
    }
}
