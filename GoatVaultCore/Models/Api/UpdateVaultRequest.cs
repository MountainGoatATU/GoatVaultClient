namespace GoatVaultCore.Models.Api;

public class UpdateVaultRequest
{
    public required VaultEncrypted Vault { get; set; }
}