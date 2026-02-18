using GoatVaultCore.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class UserRequest
{
    [JsonPropertyName("email")] public required string Email { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool? MfaEnabled { get; set; }
    [JsonPropertyName("mfa_secret")] public string? MfaSecret { get; set; }
    [JsonPropertyName("shamir_enabled")] public bool? ShamirEnabled { get; set; }
    [JsonPropertyName("vault")] public required VaultModel Vault { get; set; }
}