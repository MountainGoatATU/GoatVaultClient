using Zxcvbn;

namespace GoatVaultClient.Services.Secrets;

public static class PasswordCrackInfoService
{
    public static PasswordCrackInfo Evaluate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return new PasswordCrackInfo
            {
                Score = 0,
                CrackTimeText = null
            };
        }

        var result = Core.EvaluatePassword(password);

        return new PasswordCrackInfo
        {
            Score = result.Score,
            CrackTimeText = result.CrackTimeDisplay.OfflineSlowHashing1e4PerSecond
        };
    }
}

public struct PasswordCrackInfo
{
    public int Score { get; set; }
    public string? CrackTimeText { get; set; }
}