using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GoatVaultClient_v3.Models
{
    public class DbModel
    {
        [Key] [JsonPropertyName("_id")] public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;

        [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; } = string.Empty;

        [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; } = false;

        [JsonPropertyName("mfa_secret")] public string? MfaSecret { get; set; }

        [Column(TypeName = "TEXT")] public string VaultJson { get; set; } = string.Empty;

        [NotMapped]
        [JsonIgnore]
        public VaultPayload Vault
        {
            get => string.IsNullOrEmpty(VaultJson)
                ? new VaultPayload()
                : JsonSerializer.Deserialize<VaultPayload>(VaultJson)!;
            set => VaultJson = JsonSerializer.Serialize(value);
        }

        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
