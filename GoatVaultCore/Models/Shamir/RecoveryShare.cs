namespace GoatVaultCore.Models.Shamir;

/// <summary>
/// Represents a single share that has been entered during the recovery process.
/// </summary>
public sealed record RecoveryShare
{
    public required int Index { get; init; }
    public required string Mnemonic { get; init; }

    /// <summary>Word count for display validation feedback.</summary>
    public int WordCount => Mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    public string DisplayLabel => $"Share #{Index} ({WordCount} words)";
}