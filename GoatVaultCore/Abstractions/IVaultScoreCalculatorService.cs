using GoatVaultCore.Models;

namespace GoatVaultCore.Abstractions;

public interface IVaultScoreCalculatorService
{
    VaultScoreDetails CalculateScore(
        IEnumerable<VaultEntry>? entries,
        int masterPasswordScore,
        bool mfaEnabled);
}