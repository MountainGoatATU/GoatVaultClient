using GoatVaultCore.Services;

namespace GoatVaultTests;

public class PasswordStrengthTests
{
    private readonly PasswordStrengthService _passwordStrength = new();

    [Theory]
    [InlineData("IAtePizzaTodayAndItWasGood123!", 3, 4)]
    [InlineData("12345", 0, 1)]
    [InlineData("", 0, 0)]
    [InlineData(null, 0, 0)]
    public void Evaluate_Passwords_ReturnExpectedScoreRange(
        string? password,
        int minScore,
        int maxScore)
    {
        // Act
        var result = _passwordStrength.Evaluate(password);

        // Assert
        Assert.InRange(result.Score, minScore, maxScore);
    }
}