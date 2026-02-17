using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class AuthRegisterRequest
{
    public required string Email { get; set; }
    public required string AuthSalt { get; set; }
    public required string AuthVerifier { get; set; }
    public required VaultEncrypted? Vault { get; set; }
}