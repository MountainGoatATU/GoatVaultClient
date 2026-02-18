using GoatVaultCore.Models.Objects;
using System.IdentityModel.Tokens.Jwt;

namespace GoatVaultInfrastructure.Services.Api;

public class JwtUtils
{
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtSecurityToken ConvertJwtStringToJwtSecurityToken(string? jwt)
    {
        return string.IsNullOrWhiteSpace(jwt)
            ? throw new ArgumentException("JWT string cannot be null or empty")
            : _handler.ReadJwtToken(jwt);
    }

    public bool IsExpired(string jwt)
    {
        var token = ConvertJwtStringToJwtSecurityToken(jwt);
        return token.ValidTo <= DateTime.UtcNow;
    }
}