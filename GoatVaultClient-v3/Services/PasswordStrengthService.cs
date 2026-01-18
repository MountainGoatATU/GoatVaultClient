using Zxcvbn;

namespace GoatVaultClient_v3.Services
{
    public class PasswordStrengthService
    {
        public PasswordCrackInfo Evaluate(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return new PasswordCrackInfo
                {
                    Score = 0,
                    CrackTimeText = null
                };

            var result = Core.EvaluatePassword(password);

            return new PasswordCrackInfo
            {
                Score = result.Score,
                CrackTimeText = result.CrackTimeDisplay.OfflineFastHashing1e10PerSecond
            };
        }
    }

    public class PasswordCrackInfo
    {
        public int Score { get; set; }
        public string? CrackTimeText { get; set; }
    }
}
