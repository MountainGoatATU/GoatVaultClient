using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultCore.Services;

// TODO: Refactor
public class VaultScoreCalculatorService(
    IPasswordStrengthService passwordStrength
    ) : IVaultScoreCalculatorService
{
    public VaultScoreDetails CalculateScore(
        IEnumerable<VaultEntry>? entries,
        int masterPasswordScore, // 0-4
        bool mfaEnabled)
    {
        // Ensure entries is not null
        entries ??= [];
        var vaultEntries = entries as VaultEntry[] ?? entries.ToArray();

        // Master strength
        var masterPercent = (int)Math.Round(masterPasswordScore / 4.0 * 100, MidpointRounding.AwayFromZero);

        double foundationPoints = masterPasswordScore switch
        {
            4 => 400,
            3 => 300,
            2 => 150,
            _ => 0
        };

        var passwordList = vaultEntries
                               .Where(e => !string.IsNullOrEmpty(e.Password))
                               .ToList();

        var passwordCount = passwordList.Count;

        // Duplicates and strength
        var duplicateCount = passwordList
            .GroupBy(e => e)
            .Where(g => g
                .Count() > 1)
            .Sum(g => g
                .Count() - 1);

        var totalStrengthScore = passwordList
            .Sum(e => passwordStrength
                .Evaluate(e.Password).Score);

        var uniquenessPoints = passwordCount > 0
            ? (passwordCount - duplicateCount) / (double)passwordCount * 200
            : 200;

        var behaviorPoints = passwordCount > 0
            ? totalStrengthScore / (double)(passwordCount * 4) * 200
            : 200;

        // MFA points
        double mfaPoints = mfaEnabled ? 200 : 0;

        // Breach penalty
        var breachedPasswordsCount = passwordList
            .Count(e => !string.IsNullOrEmpty(e.Password) && e.BreachCount > 0);

        double breachPenalty = breachedPasswordsCount * 20;

        // Final score calculation
        var rawScore = foundationPoints + uniquenessPoints + behaviorPoints + mfaPoints - breachPenalty;
        if (!mfaEnabled && rawScore > 800)
            rawScore = 800;
        var finalScore = Math.Max(rawScore, 0);

        // Percentages
        var averagePercent = passwordCount > 0
            ? (int)Math.Round(totalStrengthScore / (double)passwordCount / 4.0 * 100, MidpointRounding.AwayFromZero)
            : 100;

        var reuseRatePercent = passwordCount > 0
            ? (int)Math.Round((passwordCount - duplicateCount) / (double)passwordCount * 100, MidpointRounding.AwayFromZero)
            : 100;

        return new VaultScoreDetails
        {
            VaultScore = finalScore,
            MasterPasswordPercent = masterPercent,
            AveragePasswordsPercent = averagePercent,
            ReuseRatePercent = reuseRatePercent,
            MfaEnabled = mfaEnabled,
            BreachesCount = breachedPasswordsCount,
            PasswordCount = passwordCount
        };
    }
}
