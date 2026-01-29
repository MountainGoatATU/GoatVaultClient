using GoatVaultClient.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultClient.Models;

public class LoggedInUser
{
    [JsonPropertyName("_id")] public string? Id { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("auth_salt")] public string? AuthSalt { get; set; }
    [JsonPropertyName("auth_verifier")] public string? AuthVerifier { get; set; }
    [JsonPropertyName("vault")] public VaultModel? Vault { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool? MfaEnabled { get; set; }
    [JsonPropertyName("mfa_secret")] public string? MfaSecret { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}