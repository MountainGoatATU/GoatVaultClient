using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class AuthInitResponse
    {
        [JsonPropertyName("_id")] public string UserId { get; set; }
        [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; }
        [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }
    }
}
