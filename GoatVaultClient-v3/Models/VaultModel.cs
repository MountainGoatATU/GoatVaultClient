using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Models
{
    public class VaultModel
    {
        [JsonPropertyName("vault_salt")] public string? VaultSalt { get; set; }
        [JsonPropertyName("nonce")] public string? Nonce { get; set; }
        [JsonPropertyName("encrypted_blob")] public string? EncryptedBlob { get; set; }
        [JsonPropertyName("auth_tag")] public string? AuthTag { get; set; }
    }
}
