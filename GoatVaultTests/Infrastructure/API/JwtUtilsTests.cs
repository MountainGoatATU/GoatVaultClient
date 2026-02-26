using GoatVaultInfrastructure.Services.Api;
using System.IdentityModel.Tokens.Jwt;

namespace GoatVaultTests.Infrastructure.API;

public class JwtUtilsTests
{
    private readonly JwtUtils _jwtUtils = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ConvertJwtStringToJwtSecurityToken_WhenInputIsNullOrWhitespace_ThrowsArgumentException(string? jwt)
    {
        // Act + Assert
        Assert.Throws<ArgumentException>(() => _jwtUtils.ConvertStringToSecurityToken(jwt));
    }

    [Fact]
    public void ConvertJwtStringToJwtSecurityToken_WhenInputIsValid_ReturnsToken()
    {
        // Arrange
        var jwt = CreateToken(expiresUtc: DateTime.UtcNow.AddMinutes(10));

        // Act
        var token = _jwtUtils.ConvertStringToSecurityToken(jwt);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token.RawData);
    }

    [Fact]
    public void IsExpired_WhenTokenExpiresInFuture_ReturnsFalse()
    {
        // Arrange
        var jwt = CreateToken(expiresUtc: DateTime.UtcNow.AddMinutes(10));

        // Act
        var expired = _jwtUtils.IsExpired(jwt);

        // Assert
        Assert.False(expired);
    }

    [Fact]
    public void IsExpired_WhenTokenExpiredInPast_ReturnsTrue()
    {
        // Arrange
        var jwt = CreateToken(expiresUtc: DateTime.UtcNow.AddMinutes(-10));

        // Act
        var expired = _jwtUtils.IsExpired(jwt);

        // Assert
        Assert.True(expired);
    }

    [Fact]
    public void IsExpired_WhenJwtMalformed_ThrowsException()
    {
        // Act + Assert
        Assert.ThrowsAny<Exception>(() => _jwtUtils.IsExpired("not-a-jwt"));
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
}
