namespace GoatVaultCore.Models.API;

public class UpdateUserRequest
{
    public string? AuthSalt { get; set; }
    public string? AuthVerifier { get; set; }
    public string? Email { get; set; }
    public bool? MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public string? VaultSalt { get; set; }
    public VaultEncrypted? Vault { get; set; }
}
