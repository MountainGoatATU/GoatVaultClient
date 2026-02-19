using GoatVaultInfrastructure.Services.Api;

namespace GoatVaultTests;

public class AuthTokenServiceTests
{
    [Fact]
    public void GetToken_Default_ReturnsEmptyString()
    {
        // Arrange
        var service = new AuthTokenService();

        // Act
        var token = service.GetToken();

        // Assert
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void GetRefreshToken_Default_ReturnsEmptyString()
    {
        // Arrange
        var service = new AuthTokenService();

        // Act
        var refreshToken = service.GetRefreshToken();

        // Assert
        Assert.Equal(string.Empty, refreshToken);
    }

    [Fact]
    public void SetToken_SetsToken_GetTokenReturnsSameValue()
    {
        // Arrange
        var service = new AuthTokenService();
        var token = "test-access-token";

        // Act
        service.SetToken(token);

        // Assert
        Assert.Equal(token, service.GetToken());
    }

    [Fact]
    public void SetRefreshToken_SetsRefreshToken_GetRefreshTokenReturnsSameValue()
    {
        // Arrange
        var service = new AuthTokenService();
        const string refreshToken = "test-refresh-token";

        // Act
        service.SetRefreshToken(refreshToken);

        // Assert
        Assert.Equal(refreshToken, service.GetRefreshToken());
    }

    [Fact]
    public void ClearToken_SetsTokenToEmptyString()
    {
        // Arrange
        var service = new AuthTokenService();
        service.SetToken("some-token");

        // Act
        service.ClearToken();

        // Assert
        Assert.Equal(string.Empty, service.GetToken());
    }

    [Fact]
    public void ClearRefreshToken_SetsRefreshTokenToEmptyString()
    {
        // Arrange
        var service = new AuthTokenService();
        service.SetRefreshToken("some-refresh-token");

        // Act
        service.ClearRefreshToken();

        // Assert
        Assert.Equal(string.Empty, service.GetRefreshToken());
    }

    [Fact]
    public void SetToken_ThenSetRefreshToken_BothValuesPersist()
    {
        // Arrange
        var service = new AuthTokenService();
        const string token = "access-token";
        const string refreshToken = "refresh-token";

        // Act
        service.SetToken(token);
        service.SetRefreshToken(refreshToken);

        // Assert
        Assert.Equal(token, service.GetToken());
        Assert.Equal(refreshToken, service.GetRefreshToken());
    }

    [Fact]
    public void ClearTokenAndRefreshToken_BothAreEmpty()
    {
        // Arrange
        var service = new AuthTokenService();
        service.SetToken("token");
        service.SetRefreshToken("refresh");

        // Act
        service.ClearToken();
        service.ClearRefreshToken();

        // Assert
        Assert.Equal(string.Empty, service.GetToken());
        Assert.Equal(string.Empty, service.GetRefreshToken());
    }
}
