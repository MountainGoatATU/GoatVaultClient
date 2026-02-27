using GoatVaultCore.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultCore.Services;

public class PwnedPasswordService : IPwnedPasswordService
{
    private readonly HttpClient _httpClient;
    private const string ApiUri = "https://api.pwnedpasswords.com/";

    public PwnedPasswordService() : this(CreateDefaultClient())
    {
    }

    public PwnedPasswordService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (_httpClient.BaseAddress is null)
            _httpClient.BaseAddress = new Uri(ApiUri);

        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "GoatVault");
    }

    private static HttpClient CreateDefaultClient() => new() { BaseAddress = new Uri(ApiUri) };

    // Checks if the password was breached. Returns number of breaches, or null if error.
    public async Task<int?> CheckPasswordAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        try
        {
            // SHA1 hash of password, one way hashing to avoid sending actual password to API
            var hashBytes = SHA1.HashData(Encoding.UTF8.GetBytes(password));
            var hash = Convert.ToHexString(hashBytes).ToUpperInvariant();

            // Send only first 5 chars of hash
            var prefix = hash[..5];
            var suffix = hash[5..];

            var response = await _httpClient.GetStringAsync($"range/{prefix}");
            var lines = response.Split('\n');

            return (from line in lines
                select line.Split(':')
                into parts
                where parts.Length == 2 && parts[0] == suffix
                select int.Parse(parts[1])).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
