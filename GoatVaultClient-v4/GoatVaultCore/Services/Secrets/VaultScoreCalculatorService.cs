using GoatVaultCore.Models.Vault;

namespace GoatVaultCore.Services.Secrets
{
    public sealed class VaultScoreDetails
    {
        public int MasterPercent { get; init; }
        public int MasterScore { get; init; }
        public string? MasterCrackTime { get; init; }

        public int AveragePercent { get; init; }
        public double AverageScore { get; init; }
        public int PasswordCount { get; init; }

        public double VaultScore { get; init; }
    }

    public static class VaultScoreCalculatorService
    {
        public static VaultScoreDetails CalculateScore(
            IEnumerable<VaultEntry>? entries,
            string? masterPassword)
        {
            // Master password strength              
            var masterStrength = PasswordStrengthService.Evaluate(masterPassword);

            var masterPercent = (int)Math.Round(
                (masterStrength.Score / 4.0) * 100,
                MidpointRounding.AwayFromZero);

            var masterCrackTime = string.IsNullOrWhiteSpace(masterStrength.CrackTimeText)
                ? "N/A"
                : masterStrength.CrackTimeText;

            // Average vault passwords strength
            int passwordCount = 0;
            double totalScore = 0;

            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    var strength = PasswordStrengthService.Evaluate(entry.Password);
                    totalScore += strength.Score;
                    passwordCount++;
                }
            }

            double averageScore = 0;
            int averagePercent = 0;

            if (passwordCount > 0)
            {
                averageScore = totalScore / passwordCount; // 0–4
                averagePercent = (int)Math.Round(
                    (averageScore / 4.0) * 100,
                    MidpointRounding.AwayFromZero);
            }

            
            var vaultScore = CalculateVaultScoreFromComponents(entries, masterStrength, passwordCount);

            return new VaultScoreDetails
            {
                MasterPercent = masterPercent,
                MasterScore = masterStrength.Score,
                MasterCrackTime = masterCrackTime,
                AveragePercent = averagePercent,
                AverageScore = averageScore,
                PasswordCount = passwordCount,
                VaultScore = vaultScore
            };
        }

        private static double CalculateVaultScoreFromComponents(
            IEnumerable<VaultEntry>? entries,
            PasswordStrength masterStrength,
            int passwordCount)
        {
            if (entries == null || !entries.Any())
                return 0;

            var totalAccounts = passwordCount;
            var uniquePasswords = entries.Select(e => e.Password).Distinct().Count();

            var masterEntropy = masterStrength.Score switch
            {
                4 => 80,
                3 => 60,
                2 => 40,
                1 => 20,
                _ => 10
            };

            double foundationScore = masterEntropy switch
            {
                >= 70 => 400,
                >= 50 => 300,
                >= 30 => 150,
                _ => 0
            };

            var uniquenessScore = totalAccounts == 0
                   ? 0
                   : ((double)uniquePasswords / totalAccounts) * 300;

               return Math.Max(0, uniquenessScore + foundationScore);
           }
       }
  
