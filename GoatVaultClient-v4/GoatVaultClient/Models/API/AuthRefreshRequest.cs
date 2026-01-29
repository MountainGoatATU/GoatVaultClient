using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.API;

public class AuthRefreshRequest
{
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
}