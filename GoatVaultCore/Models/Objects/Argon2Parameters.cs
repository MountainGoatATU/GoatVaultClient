namespace GoatVaultCore.Models.Objects;

public sealed record Argon2Parameters
{
    public required string Type { get; set; } // "id"
    public required int MemoryCost { get; set; }
    public required int TimeCost { get; set; }
    public required int Lanes { get; set; }
    public required int Version { get; set; }
}
