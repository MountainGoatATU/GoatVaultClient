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
            var masterPercent = (int)Math.Round((masterStrength.Score / 4.0) * 100, MidpointRounding.AwayFromZero);

            int passwordCount = 0;
            int uniquePasswordCount = 0;
            int totalScore = 0;

            if (entries != null)
            {
                var allPasswords = entries
                    .Where(e => !string.IsNullOrEmpty(e.Password))
                    .Select(e => e.Password!)
                    .ToList();

                passwordCount = allPasswords.Count;
                uniquePasswordCount = allPasswords.Distinct().Count();

                foreach (var pwd in allPasswords)
                    totalScore += PasswordStrengthService.Evaluate(pwd).Score;
            }

            int averagePercent = passwordCount > 0
                ? (int)Math.Round((totalScore / (double)passwordCount / 4.0) * 100, MidpointRounding.AwayFromZero)
                : 0;

            int reuseRatePercent = passwordCount > 0
                ? (int)Math.Round((uniquePasswordCount / (double)passwordCount) * 100, MidpointRounding.AwayFromZero)
                : 0;

            double foundation = masterStrength.Score switch
            {
                4 => 400,
                3 => 300,
                2 => 150,
                _ => 0
            };

            double uniquenessComponent = reuseRatePercent / 100.0 * 300;
            double behaviorComponent = averagePercent / 100.0 * 300;
            double breachPenalty = breachedPasswordsCount * 20;
            double mfaBonus = mfaEnabled ? 50 : 0;

            double rawScore = uniquenessComponent + behaviorComponent + foundation + mfaBonus - breachPenalty;
            double finalScore = Math.Clamp(rawScore, 0, 1000);

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
