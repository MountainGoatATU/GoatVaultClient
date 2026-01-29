using GoatVaultCore.Services.Secrets;

namespace GoatVaultTests;

public class PasswordStrengthTests
{
    [Theory]
    [InlineData("IAtePizzaTodayAndItWasGood123!", 3, int.MaxValue)]
    [InlineData("12345", 0, 1)]
    [InlineData("", 0, 0)]
    [InlineData(null, 0, 0)]
    public void Evaluate_Passwords_ReturnExpectedScoreRange(
        string? password,
        int minScore,
        int maxScore)
    {
        var result = PasswordStrengthService.Evaluate(password);

        Assert.InRange(result.Score, minScore, maxScore);
    }
}