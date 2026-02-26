using System.Net;
using System.Security.Cryptography;
using System.Text;
using GoatVaultCore.Services;
using Moq;
using Moq.Protected;

namespace GoatVaultTests.Core.Services;

public class PwnedPasswordServiceTests
{
    private const string ApiUri = "https://api.pwnedpasswords.com/";

    [Fact]
    public async Task CheckPasswordAsync_EmptyOrWhitespacePassword_ReturnsZero()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var pwnedPassword = CreateService(handlerMock);

        // Act
        var resultEmpty = await pwnedPassword.CheckPasswordAsync(string.Empty);
        var resultWhitespace = await pwnedPassword.CheckPasswordAsync("   ");

        // Assert
        Assert.Equal(0, resultEmpty);
        Assert.Equal(0, resultWhitespace);
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CheckPasswordAsync_HashSuffixExists_ReturnsBreachCount()
    {
        // Arrange
        const string password = "password123!";
        var hash = ComputeSha1(password);
        var prefix = hash[..5];
        var suffix = hash[5..];
        var responseBody = $"{suffix}:42\nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:1\n";

        var handlerMock = new Mock<HttpMessageHandler>();
        HttpRequestMessage? capturedRequest = null;

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(CreateOkResponse(responseBody));

        var pwnedPassword = CreateService(handlerMock);

        // Act
        var result = await pwnedPassword.CheckPasswordAsync(password);

        // Assert
        Assert.Equal(42, result);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal($"/range/{prefix}", capturedRequest.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task CheckPasswordAsync_HashSuffixDoesNotExist_ReturnsZero()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(CreateOkResponse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA:5\n"));

        var pwnedPassword = CreateService(handlerMock);

        // Act
        var result = await pwnedPassword.CheckPasswordAsync("another-password");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CheckPasswordAsync_WhenHttpClientThrows_ReturnsNull()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var pwnedPassword = CreateService(handlerMock);

        // Act
        var result = await pwnedPassword.CheckPasswordAsync("will-fail");

        // Assert
        Assert.Null(result);
    }

    private static string ComputeSha1(string value)
    {
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }

    private static PwnedPasswordService CreateService(Mock<HttpMessageHandler> handlerMock)
    {
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(ApiUri)
        };

        return new PwnedPasswordService(httpClient);
    }

    private static HttpResponseMessage CreateOkResponse(string body) =>
        new()
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(body)
        };
}
