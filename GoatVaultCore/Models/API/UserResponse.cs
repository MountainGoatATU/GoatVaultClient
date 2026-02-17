using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class UserResponse
{
    [JsonPropertyName("_id")] public required string Id { get; set; }
    public required string AuthSalt { get; set; }
    public required string Email { get; set; }
    public required bool MfaEnabled { get; set; }
    public required VaultEncrypted Vault { get; set; }
    [JsonPropertyName("createdAtUtc")] public required DateTime CreatedAtUtc { get; set; }
    [JsonPropertyName("updatedAtUtc")] public required DateTime UpdatedAtUtc { get; set; }
}