namespace GoatVaultCore.Models.Objects;

public sealed record Argon2Parameters
{
    public int TimeCost { get; init; } = 3;
    public int MemoryCost { get; init; } = 65536;
    public int Lanes { get; init; } = 4;
    public int Threads { get; init; } = 4;
    public int HashLength { get; init; } = 32;

    public static Argon2Parameters Default { get; } = new();
}
