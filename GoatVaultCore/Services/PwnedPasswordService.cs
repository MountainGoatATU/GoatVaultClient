using System.Security.Cryptography;
using System.Text;

namespace GoatVaultCore.Services;

public class PwnedPasswordService
{
    private readonly HttpClient _httpClient;

    public PwnedPasswordService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.pwnedpasswords.com/")
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "GoatVault");
    }

    // Checks if the password was breached. Returns number of breaches, or null if error.
    public async Task<int?> CheckPasswordAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        try
        {
            // SHA1 hash of password, one way hashing to avoid sending actual password to API
            using var sha1 = SHA1.Create();
            var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

            // Send only first 5 chars of hash
            var prefix = hash.Substring(0, 5);
            var suffix = hash.Substring(5);

            var response = await _httpClient.GetStringAsync($"range/{prefix}");
            var lines = response.Split('\n');

            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length == 2 && parts[0] == suffix)
                    return int.Parse(parts[1]);
            }
            return 0;
        }
        catch
        {
            return null;
        }
    }
}