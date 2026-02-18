using GoatVaultCore.Abstractions;
using GoatVaultCore.Models;
using Zxcvbn;

namespace GoatVaultCore.Services;

public class PasswordStrengthService : IPasswordStrengthService
{
    public PasswordStrength Evaluate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordStrength
            {
                Score = 0,
                CrackTimeText = null
            };
        }

        var result = Core.EvaluatePassword(password);

        return new PasswordStrength
        {
            Score = result.Score,
            CrackTimeText = result.CrackTimeDisplay.OfflineSlowHashing1e4PerSecond
        };
    }
}
