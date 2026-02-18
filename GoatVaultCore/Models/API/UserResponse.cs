using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class UserResponse
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
    public required string AuthSalt { get; set; }
    public required string Email { get; set; }
    public required bool MfaEnabled { get; set; }
    public required string VaultSalt { get; set; }
    public required VaultEncrypted Vault { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
    public required DateTime UpdatedAtUtc { get; set; }
}