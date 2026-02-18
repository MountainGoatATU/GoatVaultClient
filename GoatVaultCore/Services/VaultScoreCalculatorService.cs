using GoatVaultCore.Models;

namespace GoatVaultCore.Services
{
    public sealed class VaultScoreDetails
    {
        public double VaultScore { get; init; }
        public int MasterPasswordPercent { get; init; }
        public int AveragePasswordsPercent { get; init; }
        public int ReuseRatePercent { get; init; }
        public bool MfaEnabled { get; init; }
        public int BreachesCount { get; init; }
        public int PasswordCount { get; init; }
    }

    // TODO: Refactor
    public static class VaultScoreCalculatorService
    {
        private readonly IPasswordStrengthService _passwordStrengthService;

        public VaultScoreCalculatorService(IPasswordStrengthService passwordStrengthService)
        {
            _passwordStrengthService = passwordStrengthService;
        }
        public VaultScoreDetails CalculateScore(
            IEnumerable<VaultEntry> entries,
            int masterPasswordScore, // 0-4
            bool mfaEnabled,
            bool masterPasswordBreached = false)
        {
            // Ensure entries is not null
            entries ??= [];

            var total = entries.Count();
            var breached = entries.Count(e => e.BreachCount > 0);

            // Master strength
            var masterPercent = (int)Math.Round((masterPasswordScore / 4.0) * 100, MidpointRounding.AwayFromZero);

            double foundationPoints = masterPasswordScore switch
            {
                4 => 400,
                3 => 300,
                2 => 150,
                _ => 0
            };

            bool masterBreached = entries.Any(e => !string.IsNullOrEmpty(e.Password) && e.Password == masterPassword && e.BreachCount > 0);
            if (masterBreached)
            {
                foundationPoints = 0;
                masterPercent = 0;
            }

            var passwordList = entries?.Where(e => !string.IsNullOrEmpty(e.Password)).Select(e => e.Password!).ToList()
                               ?? new List<string>();
            int passwordCount = passwordList.Count;

            if (entries != null && entries.Any())
            {
                var allPasswords = entries
                    .Where(e => !string.IsNullOrEmpty(e.Password))
                    .Select(e => e.Password!)
                    .ToList();

                passwordCount = allPasswords.Count;

            double behaviorPoints = passwordCount > 0
                ? (totalStrengthScore / (double)(passwordCount * 4)) * 200
                : 200;

            // MFA points
            double mfaPoints = mfaEnabled ? 200 : 0;

            // Breach penalty
            int breachedPasswordsCount = entries?.Count(e => !string.IsNullOrEmpty(e.Password) && e.BreachCount > 0) ?? 0;
            double breachPenalty = breachedPasswordsCount * 20;

            // Final score calculation
            double rawScore = foundationPoints + uniquenessPoints + behaviorPoints + mfaPoints - breachPenalty;
            if (!mfaEnabled && rawScore > 800) rawScore = 800;
            double finalScore = Math.Max(rawScore, 0);

            // Percentages
            int averagePercent = passwordCount > 0
                ? (int)Math.Round((totalStrengthScore / (double)passwordCount / 4.0) * 100, MidpointRounding.AwayFromZero)
                : 100;

            int reuseRatePercent = passwordCount > 0
                ? (int)Math.Round(((passwordCount - duplicateCount) / (double)passwordCount) * 100, MidpointRounding.AwayFromZero)
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
}
