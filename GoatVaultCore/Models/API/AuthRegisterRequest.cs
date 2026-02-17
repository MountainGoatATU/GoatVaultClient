using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRegisterRequest
{
    [JsonPropertyName("email")] public required string Email { get; set; }
    [JsonPropertyName("auth_salt")] public required string AuthSalt { get; set; }
    [JsonPropertyName("auth_verifier")] public required string AuthVerifier { get; set; }
    [JsonPropertyName("vault_salt")] public required string VaultSalt { get; set; }
    [JsonPropertyName("vault")] public required VaultEncrypted? Vault { get; set; }
}