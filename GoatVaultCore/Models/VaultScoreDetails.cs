namespace GoatVaultCore.Models;

public sealed record VaultScoreDetails
{
    public double VaultScore { get; init; }
    public int MasterPasswordPercent { get; init; }
    public int AveragePasswordsPercent { get; init; }
    public int ReuseRatePercent { get; init; }
    public bool MfaEnabled { get; init; }
    public int BreachesCount { get; init; }
    public int PasswordCount { get; init; }
}