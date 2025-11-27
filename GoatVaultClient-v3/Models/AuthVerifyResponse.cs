using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class AuthVerifyResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }
        [JsonPropertyName("vault")] public VaultModel Vault { get; set; }
        [JsonPropertyName("token_type")] public string TokenType { get; set; }
    }
}
