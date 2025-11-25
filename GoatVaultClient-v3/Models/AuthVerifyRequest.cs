using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class AuthVerifyRequest
    {
        [JsonPropertyName("_id")] public Guid UserId { get; set; }
        [JsonPropertyName("auth_verifier")] public string AuthVerifier { get; set; }
    }
}
