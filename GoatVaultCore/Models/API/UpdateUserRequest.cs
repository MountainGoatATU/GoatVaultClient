namespace GoatVaultCore.Models.API;

public class UpdateUserRequest
{
    public VaultEncrypted? Vault { get; set; }
    public string? Email { get; set; }
    public bool? MfaEnabled { get; set; }
    public byte[]? MfaSecret { get; set; }
}
