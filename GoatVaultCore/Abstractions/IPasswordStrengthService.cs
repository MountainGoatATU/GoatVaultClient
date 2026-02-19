using GoatVaultCore.Models;

namespace GoatVaultCore.Abstractions;

public interface IPasswordStrengthService
{
    PasswordStrength Evaluate(string? password);
}