using GoatVaultCore.Models.Api;
using GoatVaultCore.Models.API;
using GoatVaultInfrastructure.Services;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

namespace GoatVaultTests.Infrastructure.Services;

public class ServerAuthServiceTests
{
    [Fact]
    public async Task InitAsync_PostsToExpectedEndpoint_AndParsesResponse()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string responseJson = """
            {"_id":"user-1","authSalt":"salt-1","nonce":"nonce-1","mfaEnabled":false}
            """;
        var handler = CreateHandler(HttpStatusCode.OK, responseJson, HttpMethod.Post, "https://api.test/v1/auth/init");
        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var result = await serverAuth.InitAsync(new AuthInitRequest { Email = "user@example.com" }, ct);

        // Assert
        Assert.Equal("user-1", result.UserId);
        Assert.Equal("salt-1", result.AuthSalt);
        Assert.Equal("nonce-1", result.Nonce);
        Assert.False(result.MfaEnabled);
    }

    [Fact]
    public async Task GetUserAsync_SendsGetToExpectedUri_AndParsesUserResponse()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var responseJson = $$"""
            {
              "_id": "{{userId}}",
              "authSalt": "AQID",
              "authVerifier": "BAUG",
              "email": "alice@example.com",
              "mfaEnabled": false,
              "mfaSecret": null,
              "shamirEnabled": false,
              "vaultSalt": "BwgJ",
              "vault": {
                "encryptedBlob": "AQID",
                "nonce": "AQIDBAUGBwgJCgsM",
                "authTag": "AQIDBAUGBwgJCgsMDQ4PEA=="
              },
              "createdAtUtc": "2025-01-01T00:00:00Z",
              "updatedAtUtc": "2025-01-01T00:00:00Z"
            }
            """;
        var handler = CreateHandler(HttpStatusCode.OK, responseJson, HttpMethod.Get, $"https://api.test/v1/users/{userId}");
        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var result = await serverAuth.GetUserAsync(userId, ct);

        // Assert
        Assert.Equal(userId.ToString(), result.Id);
        Assert.Equal("alice@example.com", result.Email);
        Assert.Equal(3, result.Vault.EncryptedBlob.Length);
        Assert.Equal(12, result.Vault.Nonce.Length);
        Assert.Equal(16, result.Vault.AuthTag.Length);
    }

    [Fact]
    public async Task UpdateUserAsync_UsesPatchMethod_AndParsesResponse()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var responseJson = $$"""
            {
              "_id": "{{userId}}",
              "authSalt": "AQID",
              "authVerifier": "BAUG",
              "email": "updated@example.com",
              "mfaEnabled": true,
              "mfaSecret": "secret",
              "shamirEnabled": true,
              "vaultSalt": "BwgJ",
              "vault": {
                "encryptedBlob": "AQID",
                "nonce": "AQIDBAUGBwgJCgsM",
                "authTag": "AQIDBAUGBwgJCgsMDQ4PEA=="
              },
              "createdAtUtc": "2025-01-01T00:00:00Z",
              "updatedAtUtc": "2025-01-01T00:00:00Z"
            }
            """;
        var handler = CreateHandler(HttpStatusCode.OK, responseJson, HttpMethod.Patch, $"https://api.test/v1/users/{userId}");
        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var result = await serverAuth.UpdateUserAsync(userId, new { mfaEnabled = true }, ct);

        // Assert
        Assert.Equal("updated@example.com", result.Email);
        Assert.True(result.MfaEnabled);
    }

    [Fact]
    public async Task AuthAndUserEndpoints_SmokeFlow_ParsesAllPrimaryResponses()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                var path = request.RequestUri!.AbsolutePath;

                if (request.Method == HttpMethod.Post && path == "/v1/auth/init")
                    return JsonResponse("""{"_id":"u1","authSalt":"salt","nonce":"n1","mfaEnabled":false}""");

                if (request.Method == HttpMethod.Post && path == "/v1/auth/verify")
                    return JsonResponse("""{"accessToken":"a","refreshToken":"r"}""");

                if (request.Method == HttpMethod.Post && path == "/v1/auth/register")
                    return JsonResponse("""{"_id":"u1","email":"new@example.com"}""");

                if (request.Method == HttpMethod.Get && path == $"/v1/users/{userId}")
                    return JsonResponse($$"""
                        {
                          "_id": "{{userId}}",
                          "authSalt": "AQID",
                          "authVerifier": "BAUG",
                          "email": "smoke@example.com",
                          "mfaEnabled": false,
                          "mfaSecret": null,
                          "shamirEnabled": false,
                          "vaultSalt": "BwgJ",
                          "vault": {
                            "encryptedBlob": "AQID",
                            "nonce": "AQIDBAUGBwgJCgsM",
                            "authTag": "AQIDBAUGBwgJCgsMDQ4PEA=="
                          },
                          "createdAtUtc": "2025-01-01T00:00:00Z",
                          "updatedAtUtc": "2025-01-01T00:00:00Z"
                        }
                        """);

                if (request.Method == HttpMethod.Patch && path == $"/v1/users/{userId}")
                    return JsonResponse($$"""
                        {
                          "_id": "{{userId}}",
                          "authSalt": "AQID",
                          "authVerifier": "BAUG",
                          "email": "patched@example.com",
                          "mfaEnabled": true,
                          "mfaSecret": "secret",
                          "shamirEnabled": false,
                          "vaultSalt": "BwgJ",
                          "vault": {
                            "encryptedBlob": "AQID",
                            "nonce": "AQIDBAUGBwgJCgsM",
                            "authTag": "AQIDBAUGBwgJCgsMDQ4PEA=="
                          },
                          "createdAtUtc": "2025-01-01T00:00:00Z",
                          "updatedAtUtc": "2025-01-01T00:00:00Z"
                        }
                        """);

                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("not found", Encoding.UTF8, "text/plain")
                };
            });

        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var init = await serverAuth.InitAsync(new AuthInitRequest { Email = "smoke@example.com" }, ct);
        var verify = await serverAuth.VerifyAsync(new AuthVerifyRequest { UserId = Guid.NewGuid(), Proof = "proof", MfaCode = null }, ct);
        var registered = await serverAuth.RegisterAsync(new AuthRegisterRequest
        {
            Email = "smoke@example.com",
            AuthSalt = "AQID",
            AuthVerifier = "BAUG",
            VaultSalt = "BwgJ",
            Argon2Parameters = GoatVaultCore.Models.Objects.Argon2Parameters.Default,
            Vault = new GoatVaultCore.Models.VaultEncrypted([1, 2, 3], [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
            {
                EncryptedBlob = [1, 2, 3],
                Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
                AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
            }
        }, ct);
        var user = await serverAuth.GetUserAsync(userId, ct);
        var updated = await serverAuth.UpdateUserAsync(userId, new { mfaEnabled = true }, ct);

        // Assert
        Assert.Equal("u1", init.UserId);
        Assert.Equal("a", verify.AccessToken);
        Assert.Equal("new@example.com", registered.Email);
        Assert.Equal("smoke@example.com", user.Email);
        Assert.Equal("patched@example.com", updated.Email);
    }

    [Fact]
    public async Task VerifyAsync_WhenValidationError422_ThrowsApiExceptionWithErrors()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string errorJson = """
            {
              "detail": [
                { "loc": ["body", "proof"], "msg": "Field required", "type": "missing" }
              ]
            }
            """;
        var handler = CreateHandler(HttpStatusCode.UnprocessableEntity, errorJson, HttpMethod.Post, "https://api.test/v1/auth/verify");
        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () =>
            await serverAuth.VerifyAsync(new AuthVerifyRequest { UserId = Guid.NewGuid(), Proof = null, MfaCode = null }, ct));

        // Assert
        Assert.Equal(422, ex.StatusCode);
        Assert.Equal("Validation Error", ex.Message);
        Assert.NotNull(ex.Errors);
        Assert.Single(ex.Errors!.Detail);
    }

    [Fact]
    public async Task RegisterAsync_WhenApiReturnsDetail_ThrowsApiExceptionWithStatusInMessage()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string errorJson = """
            {"detail":"Unauthorized"}
            """;
        var handler = CreateHandler(HttpStatusCode.Unauthorized, errorJson, HttpMethod.Post, "https://api.test/v1/auth/register");
        var serverAuth = CreateServerAuth(handler.Object);
        var payload = new AuthRegisterRequest
        {
            Email = "user@example.com",
            AuthSalt = "AQID",
            AuthVerifier = "BAUG",
            VaultSalt = "BwgJ",
            Argon2Parameters = GoatVaultCore.Models.Objects.Argon2Parameters.Default,
            Vault = new GoatVaultCore.Models.VaultEncrypted([1, 2, 3], [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12], [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1])
            {
                EncryptedBlob = [1, 2, 3],
                Nonce = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
                AuthTag = [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1]
            }
        };

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () => await serverAuth.RegisterAsync(payload, ct));

        // Assert
        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Unauthorized", ex.Message);
        Assert.Contains("401", ex.Message);
    }

    [Fact]
    public async Task VerifyAsync_When422BodyIsHtml_ThrowsApiExceptionWithRawBodyAndStatus()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string errorBody = "<html>validation failed</html>";
        var handler = CreateHandler(HttpStatusCode.UnprocessableEntity, errorBody, HttpMethod.Post, "https://api.test/v1/auth/verify", "text/html");
        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () =>
            await serverAuth.VerifyAsync(new AuthVerifyRequest { UserId = Guid.NewGuid(), Proof = "x", MfaCode = null }, ct));

        // Assert
        Assert.Equal(422, ex.StatusCode);
        Assert.Contains("validation failed", ex.Message);
        Assert.Contains("422", ex.Message);
    }

    [Fact]
    public async Task GetUserAsync_WhenServerReturnsHtmlError_ThrowsApiExceptionWithBody()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.NewGuid();
        const string errorBody = "<html>server down</html>";
        var handler = CreateHandler(HttpStatusCode.InternalServerError, errorBody, HttpMethod.Get, $"https://api.test/v1/users/{userId}", "text/html");
        var serverAuth = CreateServerAuth(handler.Object);

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () =>
            await serverAuth.GetUserAsync(userId, ct));

        // Assert
        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("server down", ex.Message);
    }

    [Fact]
    public async Task InitAsync_WhenBaseUrlMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(handler.Object);
        var config = CreateConfig(baseUrl: null);
        var serverAuth = new ServerAuthService(httpClient, config);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await serverAuth.InitAsync(new AuthInitRequest { Email = "user@example.com" }, ct));
    }

    private static Mock<HttpMessageHandler> CreateHandler(HttpStatusCode statusCode, string responseContent, HttpMethod method, string expectedUri, string mediaType = "application/json")
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r =>
                    r.Method == method &&
                    r.RequestUri != null &&
                    r.RequestUri.ToString() == expectedUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent, Encoding.UTF8, mediaType)
            });

        return handler;
    }

    private static ServerAuthService CreateServerAuth(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var config = CreateConfig(baseUrl: "https://api.test");
        return new ServerAuthService(httpClient, config);
    }

    private static Microsoft.Extensions.Configuration.IConfiguration CreateConfig(string? baseUrl)
    {
        var section = new Mock<Microsoft.Extensions.Configuration.IConfigurationSection>();
        section.SetupGet(x => x.Value).Returns(baseUrl);

        var config = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        config.Setup(x => x.GetSection("API_BASE_URL")).Returns(section.Object);

        return config.Object;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
}
