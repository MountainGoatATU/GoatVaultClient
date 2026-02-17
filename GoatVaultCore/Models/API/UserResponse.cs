using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class UserResponse
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
    [JsonPropertyName("auth_salt")] public required string AuthSalt { get; set; }
    [JsonPropertyName("auth_verifier")] public required string AuthVerifier { get; set; }
    [JsonPropertyName("email")] public required string Email { get; set; }
    [JsonPropertyName("mfa_enabled")] public required bool MfaEnabled { get; set; }
    [JsonPropertyName("mfa_secret")] public string? MfaSecret { get; set; }
    [JsonPropertyName("vault_salt")] public required string VaultSalt { get; set; }
    [JsonPropertyName("vault")] public required VaultEncrypted Vault { get; set; }
    [JsonPropertyName("created_at")] public required DateTime CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public required DateTime UpdatedAt { get; set; }
}