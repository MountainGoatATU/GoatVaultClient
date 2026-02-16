using GoatVaultCore.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultCore.Models;

public class User
{
    // User Details
    public Guid Id { get; set; } = Guid.Empty;
    public Email Email { get; set; } = null!;

    [JsonConverter(typeof(Base64Converter))]
    public byte[] AuthSalt { get; set; } = [];

    [JsonConverter(typeof(Base64Converter))]
    public byte[] AuthVerifier { get; set; } = [];

    // Vault Details
    [JsonConverter(typeof(Base64Converter))]
    public byte[] VaultSalt { get; set; } = [];
    public VaultEncrypted Vault { get; set; } = null!;

    // Timestamps
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
