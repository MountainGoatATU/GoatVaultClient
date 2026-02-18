namespace GoatVaultCore.Models.Shamir;

/// <summary>
/// AES-GCM encrypted container for the actual secret.
/// Wire format: [Nonce (12)][Tag (16)][Ciphertext (var)] → Base64.
/// </summary>
public readonly struct Envelope
{
    public required byte[] Nonce { get; init; }
    public required byte[] Tag { get; init; }
    public required byte[] Ciphertext { get; init; }

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int MinPacked = NonceSize + TagSize;

    public string ToBase64()
    {
        var packed = new byte[Nonce.Length + Tag.Length + Ciphertext.Length];
        Buffer.BlockCopy(Nonce, 0, packed, 0, Nonce.Length);
        Buffer.BlockCopy(Tag, 0, packed, Nonce.Length, Tag.Length);
        Buffer.BlockCopy(Ciphertext, 0, packed, Nonce.Length + Tag.Length, Ciphertext.Length);
        return Convert.ToBase64String(packed);
    }

    public static Envelope FromBase64(string base64)
    {
        ArgumentException.ThrowIfNullOrEmpty(base64);
        byte[] packed;
        try { packed = Convert.FromBase64String(base64); }
        catch (FormatException ex) { throw new FormatException("Invalid Base64 envelope.", ex); }

        if (packed.Length < MinPacked)
            throw new FormatException($"Envelope too short: {packed.Length} bytes, need {MinPacked}+.");

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ct = new byte[packed.Length - MinPacked];
        Buffer.BlockCopy(packed, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(packed, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(packed, MinPacked, ct, 0, ct.Length);
        return new Envelope { Nonce = nonce, Tag = tag, Ciphertext = ct };
    }
}