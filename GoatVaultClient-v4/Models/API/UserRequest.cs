using GoatVaultClient_v4.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models.API;

public class UserRequest
{
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }
    [JsonPropertyName("vault")] public VaultModel Vault { get; set; }
}