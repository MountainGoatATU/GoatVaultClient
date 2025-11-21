using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class RegisterRequest
    {
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; }
        [JsonPropertyName("auth_verifier")] public string AuthVerifier { get; set; }
        [JsonPropertyName("vault")] public VaultPayload Vault { get; set; }
    }
}
