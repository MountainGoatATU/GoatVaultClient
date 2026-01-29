using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.API;

public class AuthLogoutRequest
{
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
}