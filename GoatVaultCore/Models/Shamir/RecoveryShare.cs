using System;
using System.Collections.Generic;
using System.Text;

namespace GoatVaultCore.Models.Shamir;

public sealed record RecoveryShare
{
    public required int Index { get; init; }
    public required string Mnemonic { get; init; }

    /// <summary>Word count for display validation feedback.</summary>
    public int WordCount => Mnemonic.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    public string DisplayLabel => $"Share #{Index} ({WordCount} words)";
}
