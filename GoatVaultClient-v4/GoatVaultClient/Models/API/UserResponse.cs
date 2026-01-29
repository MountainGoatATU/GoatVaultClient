using GoatVaultClient.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.API;

public class UserResponse
{
    [JsonPropertyName("_id")] public string Id { get; set; }
    [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; }
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }
    [JsonPropertyName("vault")] public VaultModel Vault { get; set; }
}