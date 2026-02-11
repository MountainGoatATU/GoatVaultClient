using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRefreshRequest
{
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; }
}