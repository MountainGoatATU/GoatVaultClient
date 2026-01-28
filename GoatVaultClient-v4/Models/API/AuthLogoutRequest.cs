using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthLogoutRequest
{
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
}