using System.IdentityModel.Tokens.Jwt;

namespace GoatVaultInfrastructure.Services.Api;

public class JwtUtils
{
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtSecurityToken ConvertStringToSecurityToken(string? jwt)
    {
        return string.IsNullOrWhiteSpace(jwt)
            ? throw new ArgumentException("JWT string cannot be null or empty")
            : _handler.ReadJwtToken(jwt);
    }

    public bool IsExpired(string jwt)
    {
        var token = ConvertStringToSecurityToken(jwt);
        return token.ValidTo <= DateTime.UtcNow;
    }
}