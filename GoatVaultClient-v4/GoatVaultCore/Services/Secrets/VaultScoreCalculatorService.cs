using GoatVaultCore.Models.Vault;

namespace GoatVaultCore.Services.Secrets
{
    public sealed class VaultScoreDetails
    {
        // Final vault score 0–1000
        public double VaultScore { get; init; }

        // Component metrics in %
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
            int breachedPasswordsCount = 0,
            int breachedEmailsCount = 0)
        {
            // Master strength via zxcvbn
            var masterStrength = PasswordStrengthService.Evaluate(masterPassword);

            // Score: 0–4 -> percent 0–100
            var masterPercent = (int)Math.Round(
                (masterStrength.Score / 4.0) * 100,
                MidpointRounding.AwayFromZero);

            // Master entropy proxy
            var masterEntropy = masterStrength.Score switch
            {
                4 => 80,
                3 => 60,
                2 => 40,
                1 => 20,
                _ => 10
            };

            // Password list stats
            int passwordCount = 0;
            double totalScore = 0;
            int uniquePasswordCount = 0;

            if (entries != null)
            {
                var allPasswords = entries
                    .Where(e => !string.IsNullOrEmpty(e.Password))
                    .Select(e => e.Password!)
                    .ToList();

                passwordCount = allPasswords.Count;
                uniquePasswordCount = allPasswords.Distinct().Count();

                foreach (var pwd in allPasswords)
                {
                    var s = PasswordStrengthService.Evaluate(pwd);
                    totalScore += s.Score;
                }
            }

            double averageScoreRaw = 0;
            int averagePercent = 0;
            int reuseRatePercent = 0;

            if (passwordCount > 0)
            {
                averageScoreRaw = totalScore / passwordCount; // 0–4
                averagePercent = (int)Math.Round(
                    (averageScoreRaw / 4.0) * 100,
                    MidpointRounding.AwayFromZero);

                // Reuse Rate
                reuseRatePercent = (int)Math.Round(
                    ((double)uniquePasswordCount / passwordCount) * 100,
                    MidpointRounding.AwayFromZero);
            }


            var uniquenessComponent = (reuseRatePercent / 100.0) * 300.0;

            var behaviorComponent = (averagePercent / 100.0) * 300.0;

            double foundationComponent = masterEntropy switch
            {
                >= 70 => 400,
                >= 50 => 300,
                >= 30 => 150,
                _ => 0
            };

            // Breach penalty
            var breachPenalty = breachedPasswordsCount * 20;

           
            var mfaBonus = mfaEnabled ? 50 : 0;

            var rawScore = (uniquenessComponent + behaviorComponent + foundationComponent + mfaBonus)
                           - (breachPenalty);

            var finalScore = Math.Clamp(rawScore, 0, 1000);

            return new VaultScoreDetails
            {
                VaultScore = finalScore,
                ReuseRatePercent = reuseRatePercent,
                MasterPasswordPercent = masterPercent,
                AveragePasswordsPercent = averagePercent,
                MfaEnabled = mfaEnabled,
                BreachesCount = breachedPasswordsCount,
                PasswordCount = passwordCount
            };
        }
    }
}
