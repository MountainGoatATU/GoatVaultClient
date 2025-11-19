using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class UserPayload
    {
        [JsonPropertyName("_id")] public string? Id { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("auth_salt")] public string? Auth_Salt { get; set; }
        [JsonPropertyName("auth_verifier")] public string? Auth_Verifier { get; set; }
        [JsonPropertyName("vault")] public VaultPayload? Vault { get; set; }
        [JsonPropertyName("mfa_enabled")] public bool? Mfa_enabled { get; set; }
        [JsonPropertyName("mfa_secret")] public string? Mfa_secret { get; set; }
    }
}
