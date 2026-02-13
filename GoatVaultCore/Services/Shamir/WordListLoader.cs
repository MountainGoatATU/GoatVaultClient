using GoatVaultCore.Services.Shamir;
using Microsoft.VisualBasic;

namespace GoatVaultCore.Shamir.Services;

/// <summary>
/// Loads the BIP-39 wordlist from Resources/Raw/. Thread-safe, cached singleton.
/// </summary>
public static class WordListLoader
{
    private static string[]? _cached;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public static async Task<string[]> LoadAsync(string filename = "bip39words.txt")
    {
        if (_cached is not null) return _cached;

        await _lock.WaitAsync();
        try
        {
            if (_cached is not null) return _cached;

            var asssembly = typeof(WordListLoader).Assembly;

            string resourceName = "GoatVaultCore.Services.Shamir.Resources." + filename;

            using Stream? stream = asssembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
            }

            using StreamReader sr = new StreamReader(stream);
            string text = await sr.ReadToEndAsync();

            var words = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 0)
                .ToArray();

            if (words.Length != 2048)
                throw new InvalidOperationException(
                    $"Expected 2048 words in {filename}, got {words.Length}.");

            _cached = words;
            return words;
        }
        finally { _lock.Release(); }
    }

    public static async Task<Bip39MnemonicEncoder> CreateEncoderAsync(
        string filename = "bip39words.txt")
    {
        var words = await LoadAsync(filename);
        return new Bip39MnemonicEncoder(words);
    }
}