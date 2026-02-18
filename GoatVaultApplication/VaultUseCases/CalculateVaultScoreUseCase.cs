using GoatVaultCore.Abstractions;
using GoatVaultCore.Services;

namespace GoatVaultApplication.VaultUseCases;

public class CalculateVaultScoreUseCase(
    ISessionContext session,
    IUserRepository users)
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
        return VaultScoreCalculatorService.CalculateScore(
            session.Vault.Entries,
            masterPasswordStrength,
            mfaEnabled);
    }
}