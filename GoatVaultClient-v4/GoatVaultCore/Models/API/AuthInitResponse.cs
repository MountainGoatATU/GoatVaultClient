using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthInitResponse
{
    [JsonPropertyName("_id")] public required string UserId { get; set; }
    [JsonPropertyName("auth_salt")] public required string AuthSalt { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }
}