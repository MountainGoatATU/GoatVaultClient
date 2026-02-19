using GoatVaultCore.Models.Shamir;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Cryptography.ShamirsSecretSharing;
using SecretSharingDotNet.Math;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultCore.Services.Shamir;

public interface IEnvelopeSharingService
{
    List<SharePackage> Split(string secret, int totalShares, int threshold);
    string Recover(List<SharePackage> packages);
    string RecoverFromParts(string envelopeBase64, List<string> mnemonicShares);
}
public sealed class EnvelopeSharingService(IMnemonicEncoder encoder) : IEnvelopeSharingService
{
    private readonly IMnemonicEncoder _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public List<SharePackage> Split(string secret, int totalShares, int threshold)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        if (threshold < 2)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be >= 2.");
        if (totalShares < threshold)
            throw new ArgumentException($"Total shares ({totalShares}) must be >= threshold ({threshold}).");
        if (totalShares > 10)
            throw new ArgumentOutOfRangeException(nameof(totalShares), "Max 255 shares.");

        var key = RandomNumberGenerator.GetBytes(KeySize);
        try
        {
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var envelope = Encrypt(secretBytes, key);
            var envB64 = envelope.ToBase64();
            var mnemonics = SplitKey(key, totalShares, threshold);

            return mnemonics.Select((m, i) => new SharePackage
            {
                MnemonicShare = m,
                EnvelopeBase64 = envB64,
                ShareIndex = i + 1,
                Threshold = threshold,
                TotalShares = totalShares
            }).ToList();
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    public string Recover(List<SharePackage> packages)
    {
        ArgumentNullException.ThrowIfNull(packages);
        return packages.Count == 0
            ? throw new ArgumentException("Need at least one package.")
            : RecoverFromParts(packages[0].EnvelopeBase64, packages.ConvertAll(p => p.MnemonicShare));
    }

    public string RecoverFromParts(string envelopeBase64, List<string> mnemonicShares)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeBase64);
        ArgumentNullException.ThrowIfNull(mnemonicShares);

        var key = RecoverKey(mnemonicShares);
        try
        {
            var envelope = Envelope.FromBase64(envelopeBase64);
            var plain = Decrypt(envelope, key);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException(
                "Recovery failed. Shares may be insufficient, corrupted, or from a different session.", ex);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    // ── AES-GCM ──────────────────────────────────────────────────

    private static Envelope Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ct = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ct, tag);
        return new Envelope { Nonce = nonce, Tag = tag, Ciphertext = ct };
    }

    private static byte[] Decrypt(Envelope env, byte[] key)
    {
        var plain = new byte[env.Ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(env.Nonce, env.Ciphertext, env.Tag, plain);
        return plain;
    }

    // ── Shamir ───────────────────────────────────────────────────

    private List<string> SplitKey(byte[] key, int n, int k)
    {
        var splitter = new SecretSplitter<BigInteger>();
        var shares = splitter.MakeShares(k, n, key);

        var result = new List<string>(n);
        foreach (var share in shares)
        {
            var index = (byte)share.Index.Value;
            var shareValue = share.Value.Value.ToByteArray(isUnsigned: true, isBigEndian: true);

            var packed = new byte[1 + shareValue.Length];
            packed[0] = (byte)index;
            Buffer.BlockCopy(shareValue,0,packed,1, shareValue.Length);

            result.Add(_encoder.Encode(packed));
        }
        return result;
    }

    private byte[] RecoverKey(List<string> mnemonics)
    {
        // Decode each mnemonic back to the "XX-HexY" share string
        var shareStrings = mnemonics
            .Select(m =>
            {
                var decoded = _encoder.Decode(m);
                int x = decoded[0];
                var hexY = Convert.ToHexString(decoded, 1, decoded.Length - 1);
                return new Share<BigInteger>($"{x}-{hexY}");
            })
            .ToArray();

        // SecretReconstructor.Reconstruction(FinitePoint<T>[])
        // internally determines the prime from the share data
        // and performs correct modular Lagrange interpolation.
        var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();
        var combiner = new SecretReconstructor<BigInteger>(gcd);

        Shares<BigInteger> shares = shareStrings.ToArray();
        var reconstructed = combiner.Reconstruction(shares);

        // Force the output to be exactly KeySize (32 bytes)
        var rawBytes = reconstructed.ToByteArray();

        if (rawBytes.Length == KeySize) return rawBytes;

        var fixedKey = new byte[KeySize];
        if (rawBytes.Length > KeySize)
        {
            // If longer (due to library prime field), take the last 32 bytes
            Buffer.BlockCopy(rawBytes, rawBytes.Length - KeySize, fixedKey, 0, KeySize);
        }
        else
        {
            // If shorter, copy into the end of the fixedKey (left-pad with zeros)
            Buffer.BlockCopy(rawBytes, 0, fixedKey, KeySize - rawBytes.Length, rawBytes.Length);
        }
        return fixedKey;
    }
}