using GoatVaultCore.Models;
using GoatVaultCore.Services.Secrets;
using SecretSharingDotNet.Cryptography.ShamirsSecretSharing;
using SecretSharingDotNet.Math;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace GoatVaultCore.Services;

public sealed class EnvelopeSecretSharingService(IMnemonicEncoder encoder)
{
    private const int KeySizeBytes = 32;  // AES-256
    private const int NonceSizeBytes = 12; // AES-GCM standard
    private const int TagSizeBytes = 16;   // AES-GCM standard

    /// <summary>
    /// Splits a secret of any size into n mnemonic shares.
    /// Returns the envelope (encrypted secret) and the list of mnemonic shares.
    /// </summary>
    public (Envelope Envelope, List<string> MnemonicShares) Split(
        string secret, int totalShares, int threshold)
    {
        byte[] secretBytes = Encoding.UTF8.GetBytes(secret);

        // 1. Generate a random AES-256 key — this is the only thing we split
        byte[] innerKey = RandomNumberGenerator.GetBytes(KeySizeBytes);

        // 2. Encrypt the actual secret with the inner key
        Envelope envelope = Encrypt(secretBytes, innerKey);

        // 3. Split only the 32-byte inner key using Shamir
        List<string> mnemonicShares = SplitKey(innerKey, totalShares, threshold);

        // 4. Securely wipe the inner key from memory
        CryptographicOperations.ZeroMemory(innerKey);

        return (envelope, mnemonicShares);
    }

    /// <summary>
    /// Recovers the secret given the envelope and enough mnemonic shares.
    /// </summary>
    public string Recover(Envelope envelope, List<string> mnemonicShares)
    {
        // 1. Reconstruct the inner key from shares
        byte[] innerKey = RecoverKey(mnemonicShares);

        try
        {
            // 2. Decrypt the envelope
            byte[] secretBytes = Decrypt(envelope, innerKey);
            return Encoding.UTF8.GetString(secretBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(innerKey);
        }
    }

    private Envelope Encrypt(byte[] plaintext, byte[] key)
    {
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return new Envelope
        {
            Nonce = nonce,
            Tag = tag,
            Ciphertext = ciphertext
        };
    }

    private byte[] Decrypt(Envelope envelope, byte[] key)
    {
        byte[] plaintext = new byte[envelope.Ciphertext.Length];

        using var aes = new AesGcm(key, TagSizeBytes);
        aes.Decrypt(envelope.Nonce, envelope.Ciphertext, envelope.Tag, plaintext);

        return plaintext;
    }

    private List<string> SplitKey(byte[] key, int totalShares, int threshold)
    {
        var splitter = new SecretSplitter<BigInteger>();

        // Convert the 32-byte key to a hex string for SecretSharingDotNet
        string keyHex = Convert.ToHexString(key);

        var shares = splitter.MakeShares(
            (BigInteger)threshold,
            (BigInteger)totalShares,
            keyHex);

        string fullShareString = shares.ToString();
        var parts = fullShareString.Split('-');
        string thresholdStr = parts[0];

        var mnemonicShares = new List<string>();
        for (int i = 1; i < parts.Length; i++)
        {
            string individualShare = $"{thresholdStr}-{parts[i]}";
            byte[] shareBytes = Encoding.UTF8.GetBytes(individualShare);
            string mnemonic = encoder.Encode(shareBytes);
            mnemonicShares.Add(mnemonic);
        }

        return mnemonicShares;
    }

    private byte[] RecoverKey(List<string> mnemonicShares)
    {
        var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();
        var combiner = new SecretReconstructor<BigInteger>(gcd);

        var decodedParts = mnemonicShares
            .Select(m => Encoding.UTF8.GetString(encoder.Decode(m)))
            .ToList();

        string threshold = decodedParts[0].Split('-')[0];
        var xyParts = decodedParts.Select(s => s.Split('-')[1]);
        string combined = $"{threshold}-{string.Join("-", xyParts)}";

        var reconstructed = combiner.Reconstruction(combined);
        return Convert.FromHexString(reconstructed.ToString());
    }
}