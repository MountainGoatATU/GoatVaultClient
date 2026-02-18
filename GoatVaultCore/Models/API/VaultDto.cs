using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.API;

public class VaultDto
{
    [JsonConverter(typeof(Base64Converter))]
    public byte[] VaultSalt { get; set; } = [];

    [JsonConverter(typeof(Base64Converter))]
    public byte[] EncryptedBlob { get; set; } = [];

    [JsonConverter(typeof(Base64Converter))]
    public byte[] Nonce { get; set; } = [];

    [JsonConverter(typeof(Base64Converter))]
    public byte[] AuthTag { get; set; } = [];
}