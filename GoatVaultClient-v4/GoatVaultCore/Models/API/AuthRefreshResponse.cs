using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRefreshResponse
{
    [JsonPropertyName("access_token")] public required string AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public required string RefreshToken { get; set; }
    [JsonPropertyName("token_type")] public required string TokenType { get; set; }
}