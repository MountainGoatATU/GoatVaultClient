namespace GoatVaultCore.Services.Shamir;

/// <summary>
/// Loads the BIP-39 wordlist from Resources/Raw/. Thread-safe, cached singleton.
/// </summary>
public static class WordListLoader
{
    private static string[]? _cached;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public static async Task<string[]> LoadAsync(string filename = "bip39words.txt")
    {
        if (_cached is not null) return _cached;

        await Lock.WaitAsync();
        try
        {
            if (_cached is not null) return _cached;

            var assembly = typeof(WordListLoader).Assembly;

            var resourceName = "GoatVaultCore.Services.Shamir.Resources." + filename;

            await using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new FileNotFoundException($"Could not find embedded resource: {resourceName}");
            }

            using var sr = new StreamReader(stream);
            var text = await sr.ReadToEndAsync();

            var words = text
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().ToLowerInvariant())
                .Where(w => w.Length > 0)
                .ToArray();

            if (words.Length != 2048)
                throw new InvalidOperationException($"Expected 2048 words in {filename}, got {words.Length}.");

            _cached = words;
            return words;
        }
        finally { Lock.Release(); }
    }

    public static async Task<Bip39MnemonicEncoder> CreateEncoderAsync(
        string filename = "bip39words.txt")
    {
        var words = await LoadAsync(filename);
        return new Bip39MnemonicEncoder(words);
    }
}