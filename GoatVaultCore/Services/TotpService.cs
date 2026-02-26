using OtpNet;
using System.Text.RegularExpressions;

namespace GoatVaultCore.Services;

public static partial class TotpService
{
    [GeneratedRegex("[^A-Za-z2-7]")] private static partial Regex SecretRegex();
    [GeneratedRegex("^[A-Z2-7]+$")] private static partial Regex CleanSecretRegex();

    public static (string Code, int SecondsRemaining) GenerateCodeWithTime(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be null or empty", nameof(secret));

        try
        {
            var cleanSecret = CleanSecret(secret);
            var secretBytes = Base32Encoding.ToBytes(cleanSecret);
            var totp = new Totp(secretBytes, step: 30); // 30 second step (standard)

            var code = totp.ComputeTotp();
            var remainingSeconds = totp.RemainingSeconds();

            return (code, remainingSeconds);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to generate TOTP code: {ex.Message}", ex);
        }
    }

    public static bool VerifyCode(string secret, string code, int window = 1)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
            return false;

        try
        {
            var cleanSecret = CleanSecret(secret);
            var secretBytes = Base32Encoding.ToBytes(cleanSecret);
            var totp = new Totp(secretBytes);

            // Verify with time window (allows for clock skew)
            return totp.VerifyTotp(code, out var timeStepMatched, new VerificationWindow(window, window));
        }
        catch
        {
            return false;
        }
    }

    private static string CleanSecret(string secret)
    {
        // Remove spaces, dashes, and other non-alphanumeric characters
        var cleaned = SecretRegex().Replace(secret, "");

        // Convert to uppercase (Base32 is case-insensitive but typically uppercase)
        return cleaned.ToUpperInvariant();
    }

    public static bool IsValidSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
            return false;

        try
        {
            var cleanSecret = CleanSecret(secret);

            // Base32 alphabet is A-Z and 2-7
            if (!CleanSecretRegex().IsMatch(cleanSecret))
                return false;

            // Try to decode
            Base32Encoding.ToBytes(cleanSecret);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string GenerateSecret(int length = 20)
    {
        var secretBytes = KeyGeneration.GenerateRandomKey(length);
        return Base32Encoding.ToString(secretBytes);
    }
}