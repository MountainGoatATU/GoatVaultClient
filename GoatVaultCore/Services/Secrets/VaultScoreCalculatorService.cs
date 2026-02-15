using GoatVaultCore.Models.Vault;

namespace GoatVaultCore.Services.Secrets
{
    public sealed class VaultScoreDetails
    {
        public double VaultScore { get; init; }
        public int ReuseRatePercent { get; init; }
        public int MasterPasswordPercent { get; init; }
        public int AveragePasswordsPercent { get; init; }
        public bool MfaEnabled { get; init; }
        public int BreachesCount { get; init; }
        public int PasswordCount { get; init; }
    }

    public static class VaultScoreCalculatorService
    {
        public static VaultScoreDetails CalculateScore(
            IEnumerable<VaultEntry> entries,
            string masterPassword,
            bool mfaEnabled,
            bool masterPasswordBreached = false)
        {
            int total = entries.Count();
            int breached = entries.Count(e => e.BreachCount > 0);

            // Master strength via zxcvbn
            var masterStrength = PasswordStrengthService.Evaluate(masterPassword);
            var masterPercent = (int)Math.Round((masterStrength.Score / 4.0) * 100, MidpointRounding.AwayFromZero);

            double foundationPoints = masterStrength.Score switch
            {
                4 => 400,
                3 => 300,
                2 => 150,
                _ => 0
            };

            // If master password is breached, zero points
            if (masterPasswordBreached)
            {
                foundationPoints = 0;
                masterPercent = 0;
            }

            // Passwords
            var passwordCount = 0;
            var totalStrengthScore = 0;
            var duplicateCount = 0;

            if (entries != null && entries.Any())
            {
                var allPasswords = entries
                    .Where(e => !string.IsNullOrEmpty(e.Password))
                    .Select(e => e.Password!)
                    .ToList();

                passwordCount = allPasswords.Count;

                // Count duplicates for uniqueness
                duplicateCount += allPasswords.GroupBy(p => p)
                    .Where(group => group
                        .Count() > 1).Sum(group => group
                        .Count() - 1);

                // Sum password strengths
                totalStrengthScore += allPasswords
                    .Sum(pwd => PasswordStrengthService
                        .Evaluate(pwd).Score);
            }

            // Uniqueness max 200
            double uniquenessPoints;
            if (passwordCount > 0)
            {
                var uniquenessRatio = (passwordCount - duplicateCount) / (double)passwordCount;
                uniquenessPoints = uniquenessRatio * 200.0;
            }
            else
            {
                uniquenessPoints = 200; // Default 200 when no passwords
            }

            // Average password strength max 200
            var behaviorPoints = passwordCount > 0
                ? (totalStrengthScore / (double)(passwordCount * 4)) * 200
                : 200;

            var breachPenalty = breached * 20;

            var mfaPoints = mfaEnabled ? 200 : 0;

            var rawScore = foundationPoints + uniquenessPoints + behaviorPoints + mfaPoints - breachPenalty;

            if (!mfaEnabled && rawScore > 800)
                rawScore = 800;

            var finalScore = Math.Max(rawScore, 0);

            // Percentages for breakdown
            var averagePercent = passwordCount > 0
                ? (int)Math.Round((totalStrengthScore / (double)passwordCount / 4.0) * 100, MidpointRounding.AwayFromZero)
                : 100;

            var reuseRatePercent = passwordCount > 0
                ? (int)Math.Round(((passwordCount - duplicateCount) / (double)passwordCount) * 100, MidpointRounding.AwayFromZero)
                : 100;

            return new VaultScoreDetails
            {
                VaultScore = finalScore,
                MasterPasswordPercent = masterPercent,
                AveragePasswordsPercent = averagePercent,
                ReuseRatePercent = reuseRatePercent,
                MfaEnabled = mfaEnabled,
                BreachesCount = breached,
                PasswordCount = passwordCount
            };
        }
    }
}
