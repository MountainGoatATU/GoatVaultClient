// Models/Envelope.cs
namespace GoatVaultCore.Models;

/// <summary>
/// The "envelope" that travels alongside every share.
/// Everyone gets a copy; it's useless without the inner key.
/// </summary>
public sealed class Envelope
{
    public required byte[] Nonce { get; init; }       // 12 bytes for AES-GCM
    public required byte[] Tag { get; init; }         // 16 bytes authentication tag
    public required byte[] Ciphertext { get; init; }  // same length as plaintext

    /// <summary>
    /// Serialize to a base64 bundle for storage/distribution.
    /// </summary>
    public string Serialize()
    {
        // Format: [1 byte nonce len][nonce][1 byte tag len][tag][ciphertext]
        using var ms = new MemoryStream();
        ms.WriteByte((byte)Nonce.Length);
        ms.Write(Nonce);
        ms.WriteByte((byte)Tag.Length);
        ms.Write(Tag);
        ms.Write(Ciphertext);
        return Convert.ToBase64String(ms.ToArray());
    }

    public static Envelope Deserialize(string base64)
    {
        byte[] raw = Convert.FromBase64String(base64);
        using var ms = new MemoryStream(raw);

        int nonceLen = ms.ReadByte();
        byte[] nonce = new byte[nonceLen];
        ms.ReadExactly(nonce);

        int tagLen = ms.ReadByte();
        byte[] tag = new byte[tagLen];
        ms.ReadExactly(tag);

        byte[] ciphertext = new byte[ms.Length - ms.Position];
        ms.ReadExactly(ciphertext);

        return new Envelope
        {
            Nonce = nonce,
            Tag = tag,
            Ciphertext = ciphertext
        };
    }
}