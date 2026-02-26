using GoatVaultCore.Models.API;
using GoatVaultInfrastructure.Services.Api;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;

namespace GoatVaultTests.Infrastructure.API;

public class HttpServiceTests
{
    private const string TestUrl = "https://api.test/resource";

    [Fact]
    public async Task GetAsync_WhenResponseIsSuccess_ReturnsDeserializedPayload()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"name\":\"goat\",\"value\":7}", Encoding.UTF8, "application/json")
            }));
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act
        var result = await httpService.GetAsync<TestDto>(TestUrl, ct);

        // Assert
        Assert.Equal("goat", result.Name);
        Assert.Equal(7, result.Value);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task PostAsync_WhenPayloadProvided_SendsJsonBody()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
            }));
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act
        await httpService.PostAsync<SimpleOkResponse>(TestUrl, new { UserName = "Alice" }, ct);

        // Assert
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        var body = handler.LastRequestBody;
        Assert.NotNull(body);
        Assert.Contains("userName", body);
        Assert.Contains("Alice", body);
    }

    [Fact]
    public async Task SendAsync_WhenStatusIs422WithValidationPayload_ThrowsApiExceptionWithErrors()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string validationErrorJson = """
            {
              "detail": [
                { "loc": ["body", "email"], "msg": "Invalid email", "type": "value_error" }
              ]
            }
            """;
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent(validationErrorJson, Encoding.UTF8, "application/json")
            }));
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () =>
            await httpService.PostAsync<SimpleOkResponse>(TestUrl, new { }, ct));

        // Assert
        Assert.Equal(422, ex.StatusCode);
        Assert.NotNull(ex.Errors);
        Assert.Single(ex.Errors!.Detail);
        Assert.Equal("Validation Error", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenStatusIsNon422_ThrowsApiExceptionWithResponseBody()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string errorContent = "unauthorized";
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent(errorContent, Encoding.UTF8, "text/plain")
            }));
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () =>
            await httpService.GetAsync<SimpleOkResponse>(TestUrl, ct));

        // Assert
        Assert.Equal(401, ex.StatusCode);
        Assert.Contains(errorContent, ex.Message);
    }

    [Fact]
    public async Task GetAsync_WhenSuccessBodyCannotDeserialize_ThrowsException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            }));
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await httpService.GetAsync<RequiredDto>(TestUrl, ct));

        Assert.Contains("Invalid API response payload", ex.Message);
    }

    [Fact]
    public async Task SendAsync_When422HasInvalidJson_ThrowsGenericApiException()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string invalidJson = "<html>validation failed</html>";
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
            {
                Content = new StringContent(invalidJson, Encoding.UTF8, "text/html")
            }));
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act
        var ex = await Assert.ThrowsAsync<ApiException>(async () =>
            await httpService.PostAsync<SimpleOkResponse>(TestUrl, new { }, ct));

        // Assert
        Assert.Equal(422, ex.StatusCode);
        Assert.Contains("Request failed", ex.Message);
        Assert.Contains("validation failed", ex.Message);
    }

    [Fact]
    public async Task SendAsync_WhenRequestFails_DoesNotLogSensitivePayloadValues()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        const string secret = "SuperSecret123!";
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("unauthorized", Encoding.UTF8, "text/plain")
            }));
        var client = new HttpClient(handler);
        var logger = new Mock<ILogger<HttpService>>();
        var httpService = new HttpService(client, logger.Object);

        // Act
        await Assert.ThrowsAsync<ApiException>(async () =>
            await httpService.PostAsync<SimpleOkResponse>(TestUrl, new { userName = "alice", password = secret }, ct));

        // Assert
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => !state.ToString()!.Contains(secret)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenCanceled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(true);
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"name\":\"goat\",\"value\":7}", Encoding.UTF8, "application/json")
            };
        });
        var client = new HttpClient(handler);
        var httpService = new HttpService(client);

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await httpService.GetAsync<TestDto>(TestUrl, cts.Token));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return await sendAsync(request, cancellationToken);
        }
    }

    private sealed class TestDto
    {
        public required string Name { get; set; }
        public required int Value { get; set; }
    }

    private sealed class SimpleOkResponse
    {
        public required bool Ok { get; set; }
    }

    private sealed class RequiredDto
    {
        public required string Missing { get; set; }
    }
}
