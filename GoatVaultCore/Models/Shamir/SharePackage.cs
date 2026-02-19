namespace GoatVaultCore.Models.Shamir;

public sealed class SharePackage
{
    /// <summary>
    /// The mnemonic-encoded Shamir share of the AES key.
    /// ~24 words, always the same size regardless of secret length.
    /// This is the SECRET part — unique per participant.
    /// </summary>
    public required string MnemonicShare { get; init; }

    /// <summary>
    /// The encrypted secret (envelope). Identical for all participants.
    /// This is NOT secret — it's useless without enough shares.
    /// Can be stored publicly, in a shared drive, etc.
    /// </summary>
    public required string EnvelopeBase64 { get; init; }

    /// <summary>
    /// Metadata for UX purposes.
    /// </summary>
    public required int ShareIndex { get; init; }
    public required int Threshold { get; init; }
    public required int TotalShares { get; init; }
}
