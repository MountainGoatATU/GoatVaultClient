using GoatVaultCore.Models.Vault;

namespace GoatVaultCore.Services.Secrets
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

    public class VaultScoreCalculatorService
    {
        private readonly IPasswordStrengthService _passwordStrengthService;

        public VaultScoreCalculatorService(IPasswordStrengthService passwordStrengthService)
        {
            _passwordStrengthService = passwordStrengthService;
        }
        public VaultScoreDetails CalculateScore(
            IEnumerable<VaultEntry> entries,
            string masterPassword,
            bool mfaEnabled,
            bool masterPasswordBreached = false)
        {
            int total = entries.Count();
            int breached = entries.Count(e => e.BreachCount > 0);

            // Master strength via zxcvbn
            var masterStrength = _passwordStrengthService.Evaluate(masterPassword);
            var masterPercent = (int)Math.Round((masterStrength.Score / 4.0) * 100, MidpointRounding.AwayFromZero);

            double foundationPoints = masterStrength.Score switch
            {
                4 => 400,
                3 => 300,
                2 => 150,
                _ => 0
            };

            var passwordList = entries?.Where(e => !string.IsNullOrEmpty(e.Password)).Select(e => e.Password!).ToList()
                               ?? new List<string>();
            int passwordCount = passwordList.Count;

            // Duplicates and strength
            int duplicateCount = passwordList.GroupBy(p => p).Where(g => g.Count() > 1).Sum(g => g.Count() - 1);
            int totalStrengthScore = 2 * (passwordList.Sum(p => PasswordStrengthService.Evaluate(p).Score));

            double uniquenessPoints = passwordCount > 0
                ? ((passwordCount - duplicateCount) / (double)passwordCount) * 200
                : 200;

                passwordCount = allPasswords.Count;

                // Count duplicates for uniqueness
                duplicateCount += allPasswords.GroupBy(p => p)
                    .Where(group => group
                        .Count() > 1).Sum(group => group
                        .Count() - 1);

                // Sum password strengths
                totalStrengthScore += allPasswords
                    .Sum(pwd => _passwordStrengthService
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
