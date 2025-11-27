using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class UserResponse
    {
        // Map "_id" from JSON to "Id" in C#
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("auth_salt")]
        public string AuthSalt { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("mfa_enabled")]
        public bool MfaEnabled { get; set; }

        [JsonPropertyName("vault")]
        public VaultModel Vault { get; set; }
    }
}
