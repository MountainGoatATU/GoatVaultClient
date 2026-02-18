namespace GoatVaultCore.Models;

public class VaultDecrypted
{
    public List<CategoryItem> Categories { get; set; } = [];
    public List<VaultEntry> Entries { get; set; } = [];
}