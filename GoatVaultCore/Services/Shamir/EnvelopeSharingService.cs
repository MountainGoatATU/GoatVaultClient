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
public sealed class EnvelopeSharingService : IEnvelopeSharingService
{
    private readonly IMnemonicEncoder _encoder;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public EnvelopeSharingService(IMnemonicEncoder encoder)
    {
        _encoder = encoder ?? throw new ArgumentNullException(nameof(encoder));
    }

    public List<SharePackage> Split(string secret, int totalShares, int threshold)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        if (threshold < 2)
            throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be >= 2.");
        if (totalShares < threshold)
            throw new ArgumentException($"Total shares ({totalShares}) must be >= threshold ({threshold}).");
        if (totalShares > 255)
            throw new ArgumentOutOfRangeException(nameof(totalShares), "Max 255 shares.");

        byte[] key = RandomNumberGenerator.GetBytes(KeySize);
        try
        {
            byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
            var envelope = Encrypt(secretBytes, key);
            string envB64 = envelope.ToBase64();
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
        if (packages.Count == 0) throw new ArgumentException("Need at least one package.");
        return RecoverFromParts(packages[0].EnvelopeBase64, packages.Select(p => p.MnemonicShare).ToList());
    }

    public string RecoverFromParts(string envelopeBase64, List<string> mnemonicShares)
    {
        ArgumentException.ThrowIfNullOrEmpty(envelopeBase64);
        ArgumentNullException.ThrowIfNull(mnemonicShares);

        byte[] key = RecoverKey(mnemonicShares);
        try
        {
            var envelope = Envelope.FromBase64(envelopeBase64);
            byte[] plain = Decrypt(envelope, key);
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
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] ct = new byte[plaintext.Length];
        byte[] tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ct, tag);
        return new Envelope { Nonce = nonce, Tag = tag, Ciphertext = ct };
    }

    private static byte[] Decrypt(Envelope env, byte[] key)
    {
        byte[] plain = new byte[env.Ciphertext.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(env.Nonce, env.Ciphertext, env.Tag, plain);
        return plain;
    }

    // ── Shamir ───────────────────────────────────────────────────

    private List<string> SplitKey(byte[] key, int n, int k)
    {
        var secretValue = new BigInteger(key, isUnsigned: true, isBigEndian: true);

        var splitter = new SecretSplitter<BigInteger>();
        string hex = Convert.ToHexString(key);
        var shares = splitter.MakeShares((BigInteger)k, (BigInteger)n, secretValue, 256);

        var result = new List<string>(n);

        foreach(var share in shares)
        {
            // Convert Y to a 32-byte array
            byte[] yBytes = share.Value.Value.ToByteArray(isUnsigned: true, isBigEndian: true);

            // Ensure it is exactly 32 bytes (pad with leading zeros if necessary)
            byte[] paddedY = new byte[32];
            int startAt = Math.Max(0, 32 - yBytes.Length);
            Buffer.BlockCopy(yBytes, Math.Max(0, yBytes.Length - 32), paddedY, startAt, Math.Min(32, yBytes.Length));

            // Pack metadata
            byte[] packed = new byte[34];
            packed[0] = (byte)k;
            packed[1] = (byte)share.Index.Value;
            Buffer.BlockCopy(paddedY, 0, packed, 2, 32);

            result.Add(_encoder.Encode(packed));
        }
        return result;
    }

    private byte[] RecoverKey(List<string> mnemonics)
    {
        // Initiate lits of shares
        var sharePoints = new List<Share<BigInteger>>();
        int? expectedThreshold = null;

        foreach (var m in mnemonics)
        {
            byte[] packed = _encoder.Decode(m);

            if (packed.Length < 34)
                throw new FormatException($"Decoded share data is too short: expected at least 34 bytes, got {packed.Length}.");

            int treshold = packed[0];
            BigInteger x = packed[1];

            // Extract Y and convert to BigInteger
            byte[] yBytes = new byte[32];
            Buffer.BlockCopy(packed, 2, yBytes, 0, 32);
            BigInteger y = new BigInteger(yBytes, isUnsigned: true, isBigEndian: true);

            if (expectedThreshold == null) expectedThreshold = treshold;
            else if (expectedThreshold != treshold)
                throw new InvalidOperationException("Shares from different split sessions detected.");

            sharePoints.Add(new Share<BigInteger>(x,y));
        }

        var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();
        var combiner = new SecretReconstructor<BigInteger>(gcd);

        // Use the implicit operator for Share<BigInteger>[] -> Shares<BigInteger>
        Shares<BigInteger> shares = sharePoints.ToArray();
        BigInteger secret = combiner.Reconstruction(shares);

        return secret.ToByteArray(isUnsigned: true, isBigEndian: true);
    }
}