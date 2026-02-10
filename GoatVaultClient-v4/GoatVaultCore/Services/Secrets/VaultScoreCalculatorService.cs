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
            IEnumerable<VaultEntry>? entries,
            string? masterPassword,
            bool mfaEnabled,
            int breachedPasswordsCount = 0)
        {
            // Master strength via zxcvbn
            var masterStrength = PasswordStrengthService.Evaluate(masterPassword);
            int masterPercent = (int)Math.Round((masterStrength.Score / 4.0) * 100, MidpointRounding.AwayFromZero);

            double foundationPoints = masterStrength.Score switch
            {
                4 => 400,
                3 => 300,
                2 => 150,
                _ => 0
            };

            // Passwords
            int passwordCount = 0;
            int totalStrengthScore = 0;
            int duplicateCount = 0;

            if (entries != null && entries.Any())
            {
                var allPasswords = entries
                    .Where(e => !string.IsNullOrEmpty(e.Password))
                    .Select(e => e.Password!)
                    .ToList();

                passwordCount = allPasswords.Count;

                // Count duplicates for uniqueness
                foreach (var group in allPasswords.GroupBy(p => p))
                {
                    if (group.Count() > 1)
                        duplicateCount += group.Count() - 1;
                }

                // Sum password strengths
                foreach (var pwd in allPasswords)
                    totalStrengthScore += PasswordStrengthService.Evaluate(pwd).Score;
            }

            // Uniqueness max 200
            double uniquenessPoints = passwordCount > 0
                ? Math.Clamp(200 - duplicateCount, 0, 200)
                : 200;

            // Average password strength max 200
            double behaviorPoints = passwordCount > 0
                ? (totalStrengthScore / (double)(passwordCount * 4)) * 200
                : 200;

            double breachPenalty = breachedPasswordsCount * 20;

            double mfaPoints = mfaEnabled ? 200 : 0;

            double rawScore = foundationPoints + uniquenessPoints + behaviorPoints + mfaPoints - breachPenalty;

            if (!mfaEnabled && rawScore > 800)
                rawScore = 800;

            double finalScore = Math.Max(rawScore, 0);

            // Percentages for breakdown
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
