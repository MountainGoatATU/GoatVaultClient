namespace GoatVaultCore.Models;

public sealed record PasswordStrength
{
    public int Score { get; set; }
    public string? CrackTimeText { get; set; }
}