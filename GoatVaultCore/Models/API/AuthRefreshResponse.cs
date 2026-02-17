using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRefreshResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required string TokenType { get; set; }
}