using GoatVaultCore.Models.Objects;
using System.Text.Json.Serialization;

namespace GoatVaultCore.Models;

public class User
{
    // User Details
    public required Guid Id { get; set; } = Guid.Empty;
    public Email Email { get; set; } = null!;

    [JsonConverter(typeof(Base64Converter))]
    public required byte[] AuthSalt { get; set; } = [];

    [JsonConverter(typeof(Base64Converter))]
    public required byte[] AuthVerifier { get; set; } = [];

    public required bool MfaEnabled { get; set; }

    [JsonConverter(typeof(Base64Converter))]
    public required byte[] MfaSecret { get; set; } = [];

    // Vault Details
    [JsonConverter(typeof(Base64Converter))]
    public required byte[] VaultSalt { get; set; } = [];
    public VaultEncrypted Vault { get; set; } = null!;

    // Timestamps
    public required DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public required DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
