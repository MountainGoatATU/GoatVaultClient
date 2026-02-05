using GoatVaultCore.Models.Vault;

namespace GoatVaultCore.Services.Secrets
{
    public static class VaultScoreCalculatorService
    {
        public static double CalculateScore(IEnumerable<VaultEntry>? entries, double masterEntropy = 50)
        {
            if (entries == null || !entries.Any())
                return 0;

            var totalAccounts = entries.Count();
            var uniquePasswords = entries.Select(e => e.Password).Distinct().Count();

            // Foundation score (master password)
            double foundationScore = masterEntropy switch
            {
                >= 70 => 400,
                >= 50 => 300,
                >= 30 => 150,
                _ => 0
            };

            // Uniqueness score
            var uniquenessScore = ((double)uniquePasswords / totalAccounts) * 300;

            // Behavior and penalties ignored for now
            return Math.Max(0, uniquenessScore + foundationScore);
        }
    }
}
