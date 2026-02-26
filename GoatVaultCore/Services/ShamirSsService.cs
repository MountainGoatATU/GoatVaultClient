using GoatVaultCore.Abstractions;
using System.Security.Cryptography;
using System.Text;
using Xecrets.Slip39;

namespace GoatVaultCore.Services;

public class ShamirSsService(IShamirsSecretSharing sss) : IShamirSsService
{
    public List<string> SplitSecret(string secret, string passPhrase, int totalShares, int threshold)
    {
        // Convert secret to bytes
        var paddedSecretBytes = PadSecret(secret);

        // SLIP-39 enforces that the secret is a multiple of 16 bits (usually 128 to 512 bits)
        if (paddedSecretBytes.Length < 16)
            throw new ArgumentException("Secret must be at least 16 bytes (128 bits) for SLIP-39.");

        // SLIP-39 supports "Super Shamir" (groups of groups). 
        // For standard M-of-N, we use 1 group.
        var generatedShares = sss.GenerateShares(
            extendable: true,
            iterationExponent: 0,
            groupThreshold: 1,
            groups: [new Group(threshold, totalShares)],
            passphrase: passPhrase,
            masterSecret: paddedSecretBytes
        );

        // shares[0] contains the generated mnemonics for our single group
        return (from @group in generatedShares from share in @group select share.ToString()).ToList();
    }

    // <summary>
    // Recovers the variable - length secret from a subset of SLIP-39 mnemonics.
    // </summary>
    public string RecoverSecret(List<string> mnemonicShares, string passphrase)
    {
        // SLIP-39 requires at least the threshold number of shares to recover the secret.
        if (mnemonicShares == null || mnemonicShares.Count == 0)
            throw new ArgumentException("Shares are required for recovery.");

        // The SLIP-39 library will validate the shares and throw exceptions if they are invalid or insufficient.
        var allParsedShares = mnemonicShares.ConvertAll(Share.Parse);

        var sharesToCombine = allParsedShares
            .GroupBy(s => s.Prefix.GroupIndex)
            .SelectMany(group => group.Take(group.First().Prefix.MemberThreshold))
            .ToArray();

        try
        {
            // The SLIP-39 library will automatically read the threshold from the 
            // mnemonics, validate the Reed-Solomon checksums, and interpolate the Galois Field.
            var groupedShares = sss.CombineShares(sharesToCombine, passphrase);
            var recoveredPaddedSecret = groupedShares.Secret;

            if (recoveredPaddedSecret == null || recoveredPaddedSecret.Length == 0)
                throw new InvalidOperationException("Failed to recover secret. Insufficient shares provided to meet the threshold.");

            return UnpadSecret(recoveredPaddedSecret);
        }
        catch (Exception ex)
        {
            // This will catch checksum failures (e.g., mistyped words) 
            // or insufficient shares.
            throw new InvalidOperationException("Failed to recover secret. Ensure shares are correct and meet the threshold.", ex);
        }
    }
    private static byte[] PadSecret(string secret)
    {
        var rawBytes = Encoding.UTF8.GetBytes(secret);

        // We use 2 bytes at the start to store the actual length of the secret
        var requiredLength = rawBytes.Length + 2;

        // SLIP-39 requires length >= 16 bytes AND an even number of bytes
        var paddedLength = Math.Max(16, requiredLength + (requiredLength % 2));

        var padded = new byte[paddedLength];

        // Write length prefix (Big-Endian)
        padded[0] = (byte)(rawBytes.Length >> 8);
        padded[1] = (byte)(rawBytes.Length & 0xFF);

        // Copy the actual secret
        Buffer.BlockCopy(rawBytes, 0, padded, 2, rawBytes.Length);

        // Fill the remaining padded space with random noise to obfuscate length
        if (paddedLength > requiredLength)
        {
            RandomNumberGenerator.Fill(padded.AsSpan(requiredLength));
        }

        return padded;
    }

    private static string UnpadSecret(byte[] recoveredBytes)
    {
        if (recoveredBytes == null || recoveredBytes.Length < 2)
            throw new FormatException("Recovered data is too short to be a padded secret.");

        // Read the actual length of the secret from the first 2 bytes
        var originalLength = (recoveredBytes[0] << 8) | recoveredBytes[1];

        // Safety check against corruption
        if (originalLength > recoveredBytes.Length - 2 || originalLength < 0)
            throw new FormatException("Invalid padding length prefix detected. The secret may be corrupted.");

        return Encoding.UTF8.GetString(recoveredBytes, 2, originalLength);
    }

    public string TestShamir(string secret, string passphrase)
    {
        var shares = SplitSecret(secret, passphrase, totalShares: 5, threshold: 3);
        return RecoverSecret(shares, passphrase);
    }
}
