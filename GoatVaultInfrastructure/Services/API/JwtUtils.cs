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

    public DecodedToken DecodeToken(JwtSecurityToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var claims = token.Claims.Select(c => (c.Type, c.Value)).ToList();
        var audience = token.Audiences.ToList();

        return new DecodedToken(
            KeyId: token.Header.Kid ?? string.Empty,
            Issuer: token.Issuer,
            Audience: audience,
            Claims: claims,
            Expiration: token.ValidTo,
            SignatureAlgorithm: token.SignatureAlgorithm,
            RawData: token.RawData,
            Subject: token.Subject ?? string.Empty,
            ValidFrom: token.ValidFrom,
            Header: token.EncodedHeader,
            Payload: token.EncodedPayload
        );
    }

    public bool IsExpired(string jwt)
    {
        var token = ConvertJwtStringToJwtSecurityToken(jwt);
        return token.ValidTo <= DateTime.UtcNow;
    }
}