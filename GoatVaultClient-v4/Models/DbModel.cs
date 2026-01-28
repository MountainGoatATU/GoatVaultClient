using GoatVaultClient_v4.Models.Vault;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoatVaultClient_v4.Models;

public class DbModel
{
    [Key] [JsonPropertyName("_id")] public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("email")] public string Email { get; set; } = string.Empty;

    [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; } = string.Empty;

    [JsonPropertyName("mfa_enabled")] public bool MfaEnabled { get; set; }

    [JsonPropertyName("mfa_secret")] public string? MfaSecret { get; set; }

    [Column(TypeName = "TEXT")] public string VaultJson { get; set; } = string.Empty;

    [NotMapped] [JsonIgnore]
    public VaultModel Vault
    {
        get => string.IsNullOrEmpty(VaultJson)
            ? new VaultModel()
            : JsonSerializer.Deserialize<VaultModel>(VaultJson)!;
        set => VaultJson = JsonSerializer.Serialize(value);
    }

    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}