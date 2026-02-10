using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthLogoutRequest
{
    [JsonPropertyName("refresh_token")] public required string RefreshToken { get; set; }
}