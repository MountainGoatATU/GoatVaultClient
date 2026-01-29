using GoatVaultClient.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.API;

public class AuthVerifyResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; }
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
    [JsonPropertyName("token_type")] public string TokenType { get; set; }
}