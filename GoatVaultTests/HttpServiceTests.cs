using GoatVaultInfrastructure.Services.API;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace GoatVaultTests;

public class HttpServiceTests
{
    // Test helper classes
    private class TestResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    private class TestRequest
    {
        public string Email { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly HttpService _httpService;

    public HttpServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        var authTokenService = new AuthTokenService();
        var jwtUtils = new JwtUtils();
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("GOATVAULT_SERVER_BASE_URL").Value).Returns("https://api.example.com/");

        _httpService = new HttpService(_httpClient, authTokenService, jwtUtils, mockConfig.Object);
    }

    [Fact]
    public async Task GetAsync_SuccessfulRequest_ReturnsDeserializedObject()
    {
        // Arrange
        var expectedResponse = new TestResponse { Id = "123", Name = "Test" };
        var jsonResponse = JsonSerializer.Serialize(expectedResponse);

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _httpService.GetAsync<TestResponse>("https://api.example.com/test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse.Id, result.Id);
        Assert.Equal(expectedResponse.Name, result.Name);
    }

    [Fact]
    public async Task GetAsync_Unauthorized_ThrowsHttpRequestExceptionWithStatusCode()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized")
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _httpService.GetAsync<TestResponse>("https://api.example.com/test"));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task PostAsync_WithPayload_SendsCorrectData()
    {
        // Arrange
        var payload = new TestRequest { Email = "test@example.com", Password = "password123" };
        var expectedResponse = new TestResponse { Id = "456", Name = "Created" };
        var jsonResponse = JsonSerializer.Serialize(expectedResponse);

        string? capturedRequestBody = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                // Capture request body before the content is disposed
                capturedRequestBody = req.Content?.ReadAsStringAsync(ct).Result;

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(jsonResponse)
                };
            });

        // Act
        var result = await _httpService.PostAsync<TestResponse>("https://api.example.com/test", payload);

        // Assert
        Assert.NotNull(capturedRequestBody);
        var deserializedPayload = JsonSerializer.Deserialize<TestRequest>(capturedRequestBody);
        Assert.Equal(payload.Email, deserializedPayload?.Email);
        Assert.Equal(payload.Password, deserializedPayload?.Password);
    }

    [Fact]
    public async Task GetAsync_WithAuthToken_IncludesAuthorizationHeader()
    {
        // Arrange
        // This JWT has an expiration date in year 2099 (exp: 4102444800)
        const string token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiZXhwIjo0MTAyNDQ0ODAwfQ.VxUd4rNJk8LrLZjqKWVVJnLJXLlOLp8W1V9FXnJZQqk";

        var authTokenService = new AuthTokenService();
        var jwtUtils = new JwtUtils();
        authTokenService.SetToken(token);

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c.GetSection("GOATVAULT_SERVER_BASE_URL").Value).Returns("https://api.example.com/");

        var httpService = new HttpService(_httpClient, authTokenService, jwtUtils, mockConfig.Object);

        HttpRequestMessage? capturedRequest = null;

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"id\":\"1\",\"name\":\"test\"}")
                };
            });

        // Act
        await httpService.GetAsync<TestResponse>("https://api.example.com/test");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal(token, capturedRequest.Headers.Authorization.Parameter);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task PostAsync_WithErrorStatusCode_ThrowsHttpRequestException(HttpStatusCode statusCode)
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("Error occurred")
            });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => _httpService.PostAsync<TestResponse>("https://api.example.com/test", new { }));

        Assert.Equal(statusCode, exception.StatusCode);
    }
}