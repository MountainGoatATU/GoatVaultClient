namespace GoatVaultCore.Models.Vault;

public class VaultDecrypted
{
    public List<CategoryItem> Categories { get; set; } = [];
    public List<VaultEntry> Entries { get; set; } = [];
}