namespace GoatVaultCore.Models.Api;

public class AuthRegisterRequest
{
    public required string Email { get; set; }
    public required string AuthSalt { get; set; }
    public required string AuthVerifier { get; set; }
    public required string VaultSalt { get; set; }
    public required VaultEncrypted Vault { get; set; }
}