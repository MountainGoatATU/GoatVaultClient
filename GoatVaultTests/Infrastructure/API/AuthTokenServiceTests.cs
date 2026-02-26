using GoatVaultInfrastructure.Services.Api;

namespace GoatVaultTests.Infrastructure.API;

public class AuthTokenServiceTests
{
    [Fact]
    public void GetToken_Default_ReturnsEmptyString()
    {
        // Arrange
        var authTokenService = new AuthTokenService();

        // Act
        var token = authTokenService.GetToken();

        // Assert
        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public void SetToken_ThenGetToken_ReturnsSameValue()
    {
        // Arrange
        var authTokenService = new AuthTokenService();

        // Act
        authTokenService.SetToken("access-token");

        // Assert
        Assert.Equal("access-token", authTokenService.GetToken());
    }

    [Fact]
    public void SetRefreshToken_ThenGetRefreshToken_ReturnsSameValue()
    {
        // Arrange
        var authTokenService = new AuthTokenService();

        // Act
        authTokenService.SetRefreshToken("refresh-token");

        // Assert
        Assert.Equal("refresh-token", authTokenService.GetRefreshToken());
    }

    [Fact]
    public void ClearToken_ResetsTokenToEmpty()
    {
        // Arrange
        var authTokenService = new AuthTokenService();
        authTokenService.SetToken("access-token");

        // Act
        authTokenService.ClearToken();

        // Assert
        Assert.Equal(string.Empty, authTokenService.GetToken());
    }

    [Fact]
    public void ClearRefreshToken_ResetsRefreshTokenToEmpty()
    {
        // Arrange
        var authTokenService = new AuthTokenService();
        authTokenService.SetRefreshToken("refresh-token");

        // Act
        authTokenService.ClearRefreshToken();

        // Assert
        Assert.Equal(string.Empty, authTokenService.GetRefreshToken());
    }
}
