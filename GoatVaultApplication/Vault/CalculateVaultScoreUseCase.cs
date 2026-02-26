using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;

namespace GoatVaultApplication.Vault;

public class CalculateVaultScoreUseCase(
    ISessionContext session,
    IUserRepository users,
    IVaultScoreCalculatorService vaultScoreCalculator)
{
    public async Task<VaultScoreDetails> ExecuteAsync()
    {
        if (session.UserId == null || session.Vault == null)
            throw new InvalidOperationException("No user logged in or vault not loaded.");

        // Fetch user to check MFA status
        var user = await users.GetByIdAsync(session.UserId.Value);
        var mfaEnabled = user?.MfaEnabled ?? false;

        var masterPasswordStrength = session.MasterPasswordStrength;

        // Use the domain service to calculate the score
        // We need to modify the service to accept the score instead of the password string/key
        return vaultScoreCalculator.CalculateScore(
            session.Vault.Entries,
            masterPasswordStrength,
            mfaEnabled);
    }
}