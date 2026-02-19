namespace GoatVaultCore.Models;

public class VaultDecrypted
{
    public required List<CategoryItem> Categories { get; set; } = [];
    public required List<VaultEntry> Entries { get; set; } = [];
}