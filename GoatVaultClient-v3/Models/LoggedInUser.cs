using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
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
}
