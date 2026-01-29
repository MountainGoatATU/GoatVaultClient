using GoatVaultCore.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class UserRequest
{
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }
    [JsonPropertyName("vault")] public VaultModel Vault { get; set; }
}