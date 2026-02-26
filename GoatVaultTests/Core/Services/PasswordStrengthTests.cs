using GoatVaultCore.Services;

namespace GoatVaultTests.Core.Services;

public class PasswordStrengthTests
{
    private readonly PasswordStrengthService _passwordStrength = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_WhenPasswordIsNullEmptyOrWhitespace_ReturnsZeroWithNullCrackTime(string? password)
    {
        var result = _passwordStrength.Evaluate(password);

        Assert.Equal(0, result.Score);
        Assert.Null(result.CrackTimeText);
    }

    [Fact]
    public void Evaluate_WhenPasswordProvided_ReturnsScoreWithinExpectedBounds()
    {
        var result = _passwordStrength.Evaluate("IAtePizzaTodayAndItWasGood123!");

        Assert.InRange(result.Score, 0, 4);
    }

    [Fact]
    public void Evaluate_WhenPasswordProvided_ReturnsNonEmptyCrackTimeText()
    {
        var result = _passwordStrength.Evaluate("IAtePizzaTodayAndItWasGood123!");

        Assert.False(string.IsNullOrWhiteSpace(result.CrackTimeText));
    }

    [Fact]
    public void Evaluate_SamePassword_ReturnsDeterministicResult()
    {
        var first = _passwordStrength.Evaluate("ConsistentPassword123!");
        var second = _passwordStrength.Evaluate("ConsistentPassword123!");

        Assert.Equal(first.Score, second.Score);
        Assert.Equal(first.CrackTimeText, second.CrackTimeText);
    }
}
