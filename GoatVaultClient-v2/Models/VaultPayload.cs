using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GoatVaultClient_v2.Models
{
    public class VaultPayload
    {
        [JsonPropertyName("_id")] public string? Id { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("salt")] public string? Salt { get; set; }
        [JsonPropertyName("nonce")] public string? Nonce { get; set; }
        [JsonPropertyName("encrypted_blob")] public string? EncryptedBlob { get; set; }
        [JsonPropertyName("auth_tag")] public string? AuthTag { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; }
    }
}
