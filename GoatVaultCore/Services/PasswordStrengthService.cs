using Zxcvbn;

namespace GoatVaultCore.Services;

public static class PasswordStrengthService
{
    public static PasswordStrength Evaluate(string? password)
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

public struct PasswordStrength
{
    public int Score { get; set; }
    public string? CrackTimeText { get; set; }
}