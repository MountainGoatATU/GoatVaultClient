using System.Text.Json.Serialization;

namespace GoatVaultCore.Models.Api;

public class AuthInitResponse
{
    [JsonPropertyName("_id")] public required string UserId { get; set; }
    public required string AuthSalt { get; set; }
    public required string Nonce { get; set; }
    public required bool MfaEnabled { get; set; }
}