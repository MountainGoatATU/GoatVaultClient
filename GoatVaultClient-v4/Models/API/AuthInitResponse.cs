using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class AuthInitResponse
{
    [JsonPropertyName("_id")] public string UserId { get; set; }
    [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }
}