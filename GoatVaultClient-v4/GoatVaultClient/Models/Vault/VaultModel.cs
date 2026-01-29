using System.Text.Json.Serialization;

namespace GoatVaultClient.Models.Vault;

public class VaultModel
{
    [JsonPropertyName("vault_salt")] public string? VaultSalt { get; set; }
    [JsonPropertyName("nonce")] public string? Nonce { get; set; }
    [JsonPropertyName("encrypted_blob")] public string? EncryptedBlob { get; set; }
    [JsonPropertyName("auth_tag")] public string? AuthTag { get; set; }
}