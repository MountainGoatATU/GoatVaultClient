namespace GoatVaultCore.Models.API;

public class UpdateUserRequest
{
    public string? Email { get; set; }
    public bool? MfaEnabled { get; set; }
    public byte[]? MfaSecret { get; set; }
    public string? VaultSalt { get; set; }
    public VaultEncrypted? Vault { get; set; }
}
