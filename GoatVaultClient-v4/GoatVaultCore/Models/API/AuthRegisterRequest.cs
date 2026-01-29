using GoatVaultCore.Models.Vault;
using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRegisterRequest
{
    [JsonPropertyName("email")] public string Email { get; set; }
    [JsonPropertyName("auth_salt")] public string AuthSalt { get; set; }
    [JsonPropertyName("auth_verifier")] public string AuthVerifier { get; set; }
    [JsonPropertyName("vault")] public VaultModel Vault { get; set; }
}