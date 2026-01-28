using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthRefreshResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
    [JsonPropertyName("token_type")] public string TokenType { get; set; }
}